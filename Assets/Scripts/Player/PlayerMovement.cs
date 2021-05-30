using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{

    #region Properties
    [SyncVar]bool isRunning;
    PlayerEntity playerEntity;

    public float speed = 5;

    [Range(0f,0.5f)]
    public float cornerSliding = 0f;

    public SO_Bool gameStarted;

    private int currentTileX;
    private int currentTileY;

    private Vector3 currentTileCenter;
    private Vector3 movementClampLow;
    private Vector3 movementClampHigh;
    private Vector3 lastPosition;

    private Tile majorTile;
    private Tile minorTile;
    #endregion

    GenericGrid<Tile> grid;

    private void Start()
    {
        playerEntity = GetComponent<PlayerEntity>();
        lastPosition = transform.position;
    }

    public void SetWorldPosition(Vector3 position)
    {
        transform.position = position;
        UpdateTileCoordinates();
        ComputeMovementLimits();

        grid = LevelBuilder.grid;
    }
    

    void Update()
    {
        if (!gameStarted.value) return;

        if (playerEntity.hubIdentity.isLocalPlayer)
        {
            if (playerEntity.controllerDevice == null || !Application.isFocused) return;

            // 
            Vector2 moveVector = playerEntity.controllerDevice.inputs.GetMoveVector();

            float horizontal = moveVector.x;
            float vertical = moveVector.y;

            if (Mathf.Abs(horizontal) + Mathf.Abs(vertical) > 0.1f)
            {
                Move(horizontal, vertical);
                isRunning = true;
                //playerEntity.animator.SetBool("IsRunning", isRunning);
            }
            else
            {
                isRunning = false;
                //playerEntity.animator.SetBool("IsRunning", isRunning);
            }

        }
        else
        {
            Vector3 difference = transform.position - lastPosition;
            if (Mathf.Abs(difference.x) + Mathf.Abs(difference.z) > 0.01f)
            {
                UpdateTileCoordinates();
                lastPosition = transform.position;
                isRunning = true;
                //playerEntity.animator.SetBool("IsRunning", isRunning);
            }
            else if (isRunning)
            {
                isRunning = false;
                //playerEntity.animator.SetBool("IsRunning", isRunning);
            }
        }
    }

    private void Move(float h, float v)
    {
        UpdateTileCoordinates();
        ComputeMovementLimits();

        lastPosition = transform.position;
        Vector3 newPosition = lastPosition;
        float travelDistance = (Time.deltaTime * speed);
        Vector3 dir = new Vector3(h, 0, v);

        newPosition += dir.normalized * travelDistance;

        newPosition = ComputeCorners(lastPosition,newPosition, dir, travelDistance);

        newPosition.x = Mathf.Clamp(newPosition.x, movementClampLow.x, movementClampHigh.x);
        newPosition.z = Mathf.Clamp(newPosition.z, movementClampLow.z, movementClampHigh.z);


        transform.position = newPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = dbgCol;

        //Gizmos.color = Color.red;
        if (LevelBuilder.grid != null)
        {
            
            Gizmos.DrawWireCube(currentTileCenter, Vector3.one*0.95f);
        }

        //Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(debugPivot, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + debugDir);

       
        //Gizmos.color = pivotWall ? Color.red : Color.green;
        Gizmos.DrawWireSphere(testWallPivot, 0.49f);

        //Gizmos.color = destWall ? Color.magenta : Color.yellow;
        Gizmos.DrawWireCube(testWallDest, Vector3.one * 0.4f);

        //Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(intersect, 0.3f);
    }

    private void UpdateTileCoordinates()
    {
        if (grid == null) return;

        int tilex, tiley;

        grid.GetXY(transform.position, out tilex, out tiley);

        if(currentTileX != tilex || currentTileY != tiley)
        {
            if(majorTile != null && majorTile.type == ElementType.Player)
            {
                majorTile.SetType(ElementType.Empty);
            }

            Vector3 worldPosition = transform.position;
            majorTile = grid.GetGridObject(worldPosition);

            
            if (majorTile != null)
            {
                currentTileX = majorTile.x;
                currentTileY = majorTile.y;
                if(majorTile.type == ElementType.Empty)
                {
                    majorTile.SetType(ElementType.Player);
                }
                currentTileCenter = grid.GetGridObjectWorldCenter(currentTileX, currentTileY);
            }

        }
    }

    private void ComputeMovementLimits()
    {
        if (grid == null) return;

        bool leftWall = grid.GetGridObject(currentTileX - 1, currentTileY).type >= ElementType.Block;
        bool rightWall = grid.GetGridObject(currentTileX + 1, currentTileY).type >= ElementType.Block;

        bool upWall = grid.GetGridObject(currentTileX, currentTileY + 1).type >= ElementType.Block;
        bool downWall = grid.GetGridObject(currentTileX, currentTileY - 1).type >= ElementType.Block;

        float cellSize = grid.GetCellsize();

        Vector3 minOffset = new Vector3(leftWall ? 0 : -cellSize, 0 , downWall ? 0 : -cellSize);
        Vector3 maxOffset = new Vector3(rightWall ? 0 : cellSize, 0, upWall ? 0 : cellSize);

        movementClampLow = currentTileCenter + minOffset;
        movementClampHigh = currentTileCenter + maxOffset;
    }

    Vector3 debugPivot;
    Vector3 debugDir;
    Vector3 testWallPivot;
    Vector3 testWallDest;
    Vector3 intersect;
    Color dbgCol;

    bool pivotWall;
    bool destWall;
    private Vector3 ComputeCorners(Vector3 startPosition, Vector3 position , Vector3 travelDirection , float travelDistance)
    {
        dbgCol = Color.black;

        if (grid == null) return position;

        Vector3 offset = position - currentTileCenter;

        // get position sign within the Tile
        // posSignX positive = the player is on the right side of the Tile
        // posSignZ positiove = the plyaer is on the upper side of the tile
        int posSignX = Utils.RoundedSign(offset.x);
        int posSignZ = Utils.RoundedSign(offset.z);

        // the player is perfectly aligned on an axis, no turn is possible in this case
        if (posSignX == 0 || posSignZ == 0) return position;

        // sign of the travel direction on each axis
        int dirSignX = Utils.RoundedSign(travelDirection.x);
        int dirSignZ = Utils.RoundedSign(travelDirection.z);

        // detect if there is a wall to turn around it 
        // true = a blocking wall is present
        // false = the tile is empty
        bool blockingPivotTile = grid.GetGridObject(currentTileX + posSignX, currentTileY + posSignZ).type >= ElementType.Block;

        // detect is the destination tile is blocking or not
        // true = blocking
        // false = free

        bool blockingDestTile = grid.GetGridObject(currentTileX + dirSignX, currentTileY + dirSignZ).type >= ElementType.Block;

        bool blockingX = grid.GetGridObject(currentTileX + dirSignX, currentTileY).type >= ElementType.Block;
        bool blockingZ = grid.GetGridObject(currentTileX, currentTileY + dirSignZ).type >= ElementType.Block;

        testWallPivot = grid.GetGridObjectWorldCenter(currentTileX + posSignX, currentTileY + posSignZ);
        testWallDest = grid.GetGridObjectWorldCenter(currentTileX + dirSignX, currentTileY + dirSignZ);

        debugDir = Vector3.zero;
        debugPivot = Vector3.zero;
        pivotWall = blockingPivotTile;
        destWall = blockingDestTile;

        debugDir = travelDirection;


        Vector3 moveDirection = travelDirection;
        dbgCol = Color.magenta;
        // no turn is possible here, exit the function
        if (blockingPivotTile && blockingDestTile && (posSignX != dirSignX && posSignZ != dirSignZ))
        {
            dbgCol = Color.yellow;
            return position;
        }


        Vector3 newPosition = position;

        float cellSize = grid.GetCellsize();
        float radius = cellSize * 0.5f;

        bool singleAxis = dirSignX != 0 ^ dirSignZ != 0;

        // calculate the corner position around which the player will turn
        Vector3 pivot = currentTileCenter + new Vector3(radius * posSignX, 0, radius * posSignZ);
        
        if (!blockingPivotTile && blockingDestTile)
        {
            dbgCol = Color.red;
            // if the input direction (dirSign) is the same as the automatically detected direction (posSign)
            // then follow the detected direction (posSign), else the input direction has priority if it is not zero
            int signX = (posSignX == dirSignX || dirSignX == 0) ? posSignX : dirSignX;
            int signZ = (posSignZ == dirSignZ || dirSignZ == 0) ? posSignZ : dirSignZ;

            float absX = Mathf.Abs(offset.x);
            float absZ = Mathf.Abs(offset.z);

            // force the direction to follow the major axis
            if (singleAxis)
            {
                moveDirection.x = signX * (absX > absZ ? (absX > cornerSliding ? 1 : 0) : 0);
                moveDirection.z = signZ * (absX < absZ ? (absZ > cornerSliding ? 1 : 0) : 0);
            }
            else
            {
                moveDirection.x = signX * (absX > absZ ? 1 : 0);
                moveDirection.z = signZ * (absX < absZ ? 1 : 0);
            }
            

        }
        else if(blockingPivotTile && !blockingDestTile)
        {
            dbgCol = Color.green;
            
            // if the destination tile is free, set the movement vector in its direction
            Vector3 destination = grid.GetGridObjectWorldCenter(currentTileX + dirSignX, currentTileY + dirSignZ);

            moveDirection = (destination - newPosition);

            moveDirection.x = Utils.RoundedSign(moveDirection.x);
            moveDirection.z = Utils.RoundedSign(moveDirection.z);

            moveDirection.x = blockingX ? (posSignX == dirSignX) ? 0 : moveDirection.x : moveDirection.x;
            moveDirection.z = blockingZ ? (posSignZ == dirSignZ) ? 0 : moveDirection.z : moveDirection.z;
        }
        //else if(blockingPivotTile && blockingDestTile && Mathf.Abs(dirSignX - posSignX) < 2 && Mathf.Abs(dirSignZ - posSignZ) < 2)
        else if (blockingPivotTile && blockingDestTile )
        {
            dbgCol = Color.blue;
            // if both pivot and destination tiles are blocking
            // force the direction on the major axis (to prevent slowdown while moving in diagonal against walls)
            moveDirection.x = (blockingX ? (posSignX == dirSignX) ? 0 : dirSignX : dirSignX) * 1.05f; // add 5% to prevent locking when moving perfectly in diagonal against a corner
            moveDirection.z = blockingZ ? (posSignZ == dirSignZ) ? 0 : dirSignZ : dirSignZ;
        }
        else if (posSignX != 0 && posSignZ != 0)
        {
            // moving diagonally
            dbgCol = Color.cyan;
            moveDirection.x = blockingX ? (posSignX == dirSignX) ? 0 : dirSignX : dirSignX;
            moveDirection.z = blockingZ ? (posSignZ == dirSignZ) ? 0 : dirSignZ : dirSignZ;
        }

        // compute the new position according to the defined direction
        newPosition = startPosition + moveDirection.normalized * travelDistance;


        intersect = newPosition;

        // test if the new position is below the corners radius threshold
        float distance = Vector3.Distance(newPosition, pivot);
        if (distance < radius)
        {
            // push back the position on the corner radius
            Vector3 pushDirection = (newPosition - pivot).normalized;
            newPosition = (pivot + pushDirection * radius);

            // define the final position according to the allowed travel distance from the start point
            newPosition = startPosition + (newPosition - startPosition).normalized * travelDistance;
        }

        debugDir = moveDirection;
            
        debugPivot = pivot;
 
        
        return newPosition;
    }
}

