using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum SpecialMove 
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero; 
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.5f;
    [SerializeField] private float dragOffset = 1.0f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField]private Transform rematchIndicator;
    [SerializeField]private Button rematchButton;


    [Header("Prefabs & Material")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] material ;

    //LOGIC
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks  = new List<ChessPiece>();
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private ChessPiece currentlyDragging;
    private ChessPiece[,] chessPieces;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;

    // Multi Logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    private void Start()
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();

        RegisterEvents();
    }
    private void Update()
    {
    if (!currentCamera)
    {
        currentCamera = Camera.main;
        return;
    }

    RaycastHit info;
    Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
    {
        // Get the indexes of the tile i've hit
        Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

        // If we're hovering a tile after not hovering any tiles
        if (currentHover == -Vector2Int.one)
        {
            currentHover = hitPosition;
            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
        }

        // If we were already hovering a tile, change the previous one
        if (currentHover != hitPosition)
        {
            tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
            currentHover = hitPosition;
            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
        }

        if(Input.GetMouseButtonDown(0))
        {
            if(chessPieces[hitPosition.x, hitPosition.y] != null)
            {
                //Is it our turn
                if((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1))
                {
                    currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                    //Get a list, where i can go
                    availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    //Get a list of special moves
                    specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                    PreventCheck();

                    HighlightTiles();
                }
            }
        }

        if(currentlyDragging != null && Input.GetMouseButtonUp(0))
        {
            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

            if(ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y))) 
            { 
                MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                //Net implementation
                NetMakeMove mm = new NetMakeMove();
                mm.originalX = previousPosition.x;
                mm.originalY = previousPosition.y;
                mm.destinationX = hitPosition.x;
                mm.destinationY = hitPosition.y;
                mm.teamId = currentTeam;
                Client.Instance.SendToServer(mm);
            }
            else
            {
                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
    }
    else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

    if(currentlyDragging)
    {
        Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
        float distance = 0.0f;
        if(horizontalPlane.Raycast(ray, out distance))
        {
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }
}


    //GENERATE THE BOARD
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for(int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1 )* tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1 )* tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();


        return tileObject;
    }

    //SPAWNING PIECES
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0;
        int blackTeam = 1;

        //WHITE TEAM
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //BLACK TEAM
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }

    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = material[team];
        
        return cp;
    }

    //POSITIONING
    private void PositionAllPieces()
    {
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // highlights tiles
    private void  HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++){
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

        private void  RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++){
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }

        availableMoves.Clear();
    }

    // Checkmate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    // Display Victory
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnRematchButton()
    {
        if(localGame)
        {
        NetRematch wrm = new NetRematch();
        wrm.teamId = 0;
        wrm.wantRematch = 1;
        Client.Instance.SendToServer(wrm);

        NetRematch brm = new NetRematch();
        brm.teamId = 1;
        brm.wantRematch = 1;
        Client.Instance.SendToServer(brm);
        }
        else
        {
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 1;
        Client.Instance.SendToServer(rm);
        }
    }
    public void GameReset()
    {
        // UI
        rematchButton.interactable = true;

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Fields reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // Clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x, y]!=null)
                Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);

        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        GameReset();
        GameUI.Instance.OnLeaveFromMenu();

        Invoke("ShutdownRelay", 1.0f);
        //Reset values
        playerCount = -1;
        currentTeam = -1;
    }

    //Special moves
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for  (int x =  0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
               if(chessPieces[x,y] != null)
               {
                    if(chessPieces[x,y].team ==  targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if(chessPieces[x,y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
               }
            }
        }

        // Is the king attacked right now
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for(int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }
        if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            //King is under attack, can we move something to help him?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if(defendingMoves.Count != 0)
                    return false;
            }

            return true; //Checkmate Exit
        }

        return false;
    }

    private void PreventCheck()
    {
        ChessPiece targetKing = null;

        for  (int x =  0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
               if(chessPieces[x,y] != null)
                    if(chessPieces[x,y].type == ChessPieceType.King)
                        if(chessPieces[x,y].team ==  currentlyDragging.team)
                            targetKing = chessPieces[x,y];
            }
        }
        
        //Since we're sending ref availableMoves, we will be deleting moves that are putting us in check
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {   
        //Save current values, to reset after function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Going through all the moves, simulate them and check if we're in check
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //Did we simulate the king's move
            if(cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX, simY);

            // Copy the [,] and not a ref
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for  (int x =  0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x,y] != null)
                    {
                        simulation[x,y] = chessPieces[x,y];
                        if (simulation[x,y].team != cp.team)
                            simAttackingPieces.Add(simulation[x,y]);
                    }
                }
            }

        // Simulate that move
        simulation[actualX, actualY] = null;
        cp.currentX = simX;
        cp.currentY = simY;
        simulation[simX, simY] = cp;

        // Did one of the piece got taken down during our simulation
        var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
        if (deadPiece != null)
            simAttackingPieces.Remove(deadPiece);

        //Get all the simulated attacking pieces moves
        List<Vector2Int> simMoves = new List<Vector2Int>();
        for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }
            }

        // Is the king in trouble? If so, remove the move
        if(ContainsValidMove(ref simMoves, kingPositionThisSim))
        {
            movesToRemove.Add(moves[i]);
        }
        //Restore the actual CP data
        cp.currentX = actualX;
        cp.currentY = actualY;    
        }

        //Remove from current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }

    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if(enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8*tileSize, yOffset, -1 * tileSize)
                            - bounds 
                            + new Vector3(tileSize / 2, 0, tileSize / 2) 
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1*tileSize, yOffset, 8 * tileSize)
                            - bounds 
                            + new Vector3(tileSize / 2, 0, tileSize / 2) 
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                //White team
                if(targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                //Black team
                if(targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }

        if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            //Lest Rook
            if(lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3,0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3,7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            //Right Rook
            else if(lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5,0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5,7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    
    //OPERATION
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(tiles[x,y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return -Vector2Int.one;
    }

    
    private void MoveTo(int originalX, int originalY, int x, int y)
{
    ChessPiece cp = chessPieces[originalX, originalY]; 
    Vector2Int previousPosition = new Vector2Int(originalX, originalY);

    if (chessPieces[x, y] != null)
    {
        ChessPiece ocp = chessPieces[x, y];
        if (cp.team == ocp.team)
        {
            return;
        }

        if (ocp.team == 0)
        {
            if (ocp.type == ChessPieceType.King)
                return; // Нельзя захватить короля

            deadWhites.Add(ocp);
            ocp.SetScale(Vector3.one * deathSize);
            ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else
        {
            if (ocp.type == ChessPieceType.King)
                return; // Нельзя захватить короля

            deadBlacks.Add(ocp);
            ocp.SetScale(Vector3.one * deathSize);
            ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }
    }

    chessPieces[x, y] = cp;
    chessPieces[previousPosition.x, previousPosition.y] = null;

    PositionSinglePiece(x, y);

    isWhiteTurn = !isWhiteTurn;
    if (localGame)
        currentTeam = (currentTeam == 0) ? 1 : 0;

    moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

    ProcessSpecialMove();

    if (currentlyDragging)
        currentlyDragging = null;
    RemoveHighlightTiles();

    if (CheckForCheckmate())
    {
        CheckMate(cp.team);
        return; // Игра завершена, блокируем дальнейшие ходы
    }

    // Если это локальная игра и ход теперь за компьютером
    if (localGame && currentTeam == 1) // Предполагаем, что 1 - команда компьютера
    {
        StartCoroutine(PerformComputerMoveWithDelay());
    }

    return;
}

private IEnumerator PerformComputerMoveWithDelay()
{
    yield return new WaitForSeconds(1f); // Задержка перед ходом компьютера
    PerformComputerMove();
}

private void PerformComputerMove()
{
    // Список всех доступных ходов для команды компьютера
    List<Vector2Int[]> availableMoves = GetAllAvailableMoves(currentTeam);

    if (availableMoves.Count > 0)
    {
        Vector2Int[] bestMove = GetBestMove(availableMoves);
        Vector2Int from = bestMove[0];
        Vector2Int to = bestMove[1];

        // Выполняем ход
        MoveTo(from.x, from.y, to.x, to.y);
    }
}

private List<Vector2Int[]> GetAllAvailableMoves(int team)
{
    List<Vector2Int[]> moves = new List<Vector2Int[]>();

    for (int x = 0; x < 8; x++)
    {
        for (int y = 0; y < 8; y++)
        {
            ChessPiece cp = chessPieces[x, y];
            if (cp != null && cp.team == team)
            {
                List<Vector2Int> pieceMoves = cp.GetAvailableMoves(ref chessPieces, 8, 8);
                SimulateMoveForSinglePiece(cp, ref pieceMoves, FindKing(team)); // Проверка на шах
                foreach (var move in pieceMoves)
                {
                    moves.Add(new Vector2Int[] { new Vector2Int(x, y), move });
                }
            }
        }
    }

    return moves;
}

private Vector2Int[] GetBestMove(List<Vector2Int[]> availableMoves)
{
    // Определяем вероятность выбора случайного хода
    float randomMoveChance = 0.3f; // 30% шанс на случайный ход

    if (UnityEngine.Random.value < randomMoveChance)
    {
        // Возвращаем случайный ход
        return availableMoves[UnityEngine.Random.Range(0, availableMoves.Count)];
    }

    // Логика для выбора лучшего хода
    List<Vector2Int[]> bestMoves = new List<Vector2Int[]>();
    int maxScore = int.MinValue;

    foreach (var move in availableMoves)
    {
        Vector2Int from = move[0];
        Vector2Int to = move[1];
        ChessPiece target = chessPieces[to.x, to.y];

        int moveScore = 0;

        // Если ход захватывает фигуру, оцениваем его выше
        if (target != null)
        {
            moveScore += GetPieceValue(target.type);
        }

        if (moveScore > maxScore)
        {
            maxScore = moveScore;
            bestMoves.Clear();
            bestMoves.Add(move);
        }
        else if (moveScore == maxScore)
        {
            bestMoves.Add(move);
        }
    }

    // Выбираем случайный ход из лучших
    return bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];
}

private ChessPiece FindKing(int team)
{
    for (int x = 0; x < TILE_COUNT_X; x++)
    {
        for (int y = 0; y < TILE_COUNT_Y; y++)
        {
            if (chessPieces[x, y] != null && chessPieces[x, y].team == team && chessPieces[x, y].type == ChessPieceType.King)
                return chessPieces[x, y];
        }
    }
    return null;
}

private int GetPieceValue(ChessPieceType type)
{
    switch (type)
    {
        case ChessPieceType.Pawn:
            return 10;
        case ChessPieceType.Knight:
        case ChessPieceType.Bishop:
            return 30;
        case ChessPieceType.Rook:
            return 50;
        case ChessPieceType.Queen:
            return 90;
        case ChessPieceType.King:
            return 900;
        default:
            return 0;
    }
}


    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

    #region 
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SETLocalGame += OnSetLocalGame;
    }

    private void UnregisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.SETLocalGame -= OnSetLocalGame;
    }

    // Server
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove mm = msg as NetMakeMove;

        //Could do some validation
        //--

        // Receive, and just broadcast it back
        Server.Instance.Broadcast(mm);
    }

    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        // Client has connected, assigned team and return the message back 
        NetWelcome nw = msg as NetWelcome;

        //Assign a team
        nw.AssignedTeam = ++playerCount;

        // Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        // if full, start the game
        if(playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }   
    }

    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    // Client
    private void OnWelcomeClient(NetMessage msg)
    {
        // Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        //Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is { nw. AssignedTeam}");

        if (localGame && currentTeam == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }

    private void OnStartGameClient(NetMessage msg)
    {
        // We just need to change the camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? cameraAngle.whiteTeam : cameraAngle.blackTeam);
    }

    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        Debug.Log($"MM : {mm.teamId} : {mm.originalX} {mm.destinationY} -> {mm.destinationX} {mm.destinationY}");

        if (mm.teamId != currentTeam)
        {
            ChessPiece target = chessPieces[mm.originalX, mm.originalY];

            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }
    }
    private void OnRematchClient(NetMessage msg)
    {
        //Receive the con msg
        NetRematch rm = msg as NetRematch;

        //Set bool for rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;
        
        //Activate the piece of UI
        if(rm.teamId!=currentTeam)
        {
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if(rm.wantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }
        //If both wants to rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }

    // Set Local Game
    private void OnSetLocalGame(bool v)
    {
        playerCount = -1;
        currentTeam = -1;

        localGame = v;
    }
    #endregion
}