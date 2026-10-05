using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;

const int SCREEN_WIDTH = 624 * 2;
const int SCREEN_HEIGHT = 624;
const int VIEWPORT = SCREEN_WIDTH / 2;

const int mapWidth = 24;
const int mapHeight = 24;

const int gridWidth = VIEWPORT / mapWidth;

InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Vector");

// buffer
Color[] buffer = new Color[VIEWPORT * SCREEN_HEIGHT];
Image image = GenImageColor(VIEWPORT, SCREEN_HEIGHT, Color.SkyBlue);
Image textureWall = LoadImage("./greystone.png"); // image from Wolf3D

Texture2D texture = LoadTextureFromImage(image);
UnloadImage(image);
SetTextureFilter(texture, TextureFilter.Point);

// TODO generate map using backtracking algo

byte[,] map = new byte[mapWidth, mapHeight]{
  {4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,7,7,7,7,7,7,7,7},
  {4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7,0,0,0,0,0,0,7},
  {4,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7},
  {4,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7},
  {4,0,3,0,0,0,0,0,0,0,0,0,0,0,0,0,7,0,0,0,0,0,0,7},
  {4,0,4,0,0,0,0,5,5,5,5,5,5,5,5,5,7,7,0,7,7,7,7,7},
  {4,0,5,0,0,0,0,5,0,5,0,5,0,5,0,5,7,0,0,0,7,7,7,1},
  {4,0,6,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,0,0,0,8},
  {4,0,7,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7,7,7,1},
  {4,0,8,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,0,0,0,8},
  {4,0,0,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,7,7,7,1},
  {4,0,0,0,0,0,0,5,5,5,5,0,5,5,5,5,7,7,7,7,7,7,7,1},
  {6,6,6,6,6,6,6,6,6,6,6,0,6,6,6,6,6,6,6,6,6,6,6,6},
  {8,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4},
  {6,6,6,6,6,6,0,6,6,6,6,0,6,6,6,6,6,6,6,6,6,6,6,6},
  {4,4,4,4,4,4,0,4,4,4,6,0,6,2,2,2,2,2,2,2,3,3,3,3},
  {4,0,0,0,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,0,0,0,2},
  {4,0,0,0,0,0,0,0,0,0,0,0,6,2,0,0,5,0,0,2,0,0,0,2},
  {4,0,0,0,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,2,0,2,2},
  {4,0,6,0,6,0,0,0,0,4,6,0,0,0,0,0,5,0,0,0,0,0,0,2},
  {4,0,0,5,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,2,0,2,2},
  {4,0,6,0,6,0,0,0,0,4,6,0,6,2,0,0,5,0,0,2,0,0,0,2},
  {4,0,0,0,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,0,0,0,2},
  {4,4,4,4,4,4,4,4,4,4,1,1,1,2,2,2,2,2,2,3,3,3,3,3}
};

Vector2 player = new Vector2(1.1f, 1.1f);   // player position vector
Vector2 dir = new Vector2(-1, 0);           // player direction of view
Vector2 plane = new Vector2(0, 0.66f);      // perpendicular plane to direction of view
var originScreen = new Vector2(0, 0);       // 0,0 is bottom, left

// var FOV = plane.Length() / dir.Length();

while (!WindowShouldClose())
{
    Array.Clear(buffer);
    Array.Fill(buffer, Color.Black, 0, buffer.Length / 2);
    Array.Fill(buffer, Color.DarkGray, buffer.Length / 2, buffer.Length / 2);
    var dt = GetFrameTime();
    double rotSpeed = dt * 3.0;
    double moveSpeed = dt * 3.0;

    // update
    float playerMovRadius = 0.25f; // don't allow the player to come too close to the walls
    if (IsKeyDown(KeyboardKey.W))
        TryMove(dir * (float)moveSpeed, playerMovRadius);
    else if (IsKeyDown(KeyboardKey.S))
        TryMove(-dir * (float)moveSpeed, playerMovRadius);

    if (IsKeyDown(KeyboardKey.A))
        TryMove(new Vector2(-dir.Y, dir.X) * (float)moveSpeed, playerMovRadius);   // (x,y) rotate 90 = (-y,x)
    else if (IsKeyDown(KeyboardKey.D))
        TryMove(new Vector2(dir.Y, -dir.X) * (float)moveSpeed, playerMovRadius);   // (x,y) rotate -90 = (y,-x)

    if (IsKeyDown(KeyboardKey.Left))
    {
        double oldDirX = dir.X;
        dir.X = (float)(dir.X * Math.Cos(rotSpeed) - dir.Y * Math.Sin(rotSpeed));           // TODO check Transform and Quaternions
        dir.Y = (float)(oldDirX * Math.Sin(rotSpeed) + dir.Y * Math.Cos(rotSpeed));
        double oldPlaneX = plane.X;
        plane.X = (float)(plane.X * Math.Cos(rotSpeed) - plane.Y * Math.Sin(rotSpeed));
        plane.Y = (float)(oldPlaneX * Math.Sin(rotSpeed) + plane.Y * Math.Cos(rotSpeed));
    }
    else if (IsKeyDown(KeyboardKey.Right))
    {
        double oldDirX = dir.X;
        dir.X = (float)(dir.X * Math.Cos(-rotSpeed) - dir.Y * Math.Sin(-rotSpeed));
        dir.Y = (float)(oldDirX * Math.Sin(-rotSpeed) + dir.Y * Math.Cos(-rotSpeed));
        double oldPlaneX = plane.X;
        plane.X = (float)(plane.X * Math.Cos(-rotSpeed) - plane.Y * Math.Sin(-rotSpeed));
        plane.Y = (float)(oldPlaneX * Math.Sin(-rotSpeed) + plane.Y * Math.Cos(-rotSpeed));
    }

    BeginDrawing();
    ClearBackground(new Color(0x0C, 0x0C, 0x0C));

    // draw map
    for (int row = 0; row < mapHeight; row++)
    {
        for (int col = 0; col < mapWidth; col++)
        {
            int sx = col * gridWidth;
            int sy = row * gridWidth;
            if (map[row, col] != 0)
                DrawRectangle(sx, sy, gridWidth, gridWidth, Color.White);
            else
                DrawRectangleLines(sx, sy, gridWidth, gridWidth, Color.White);
        }
    }
    DrawVector(originScreen, player, Color.Red);

    DrawVector(player, plane, Color.Green);


    // raycasting
    // ray starts at player
    for (int x = 0; x < VIEWPORT; x++)
    {
        float cameraX = 2 * (float)x / VIEWPORT - 1;    // camera space = pos + dir - plane to pos + dir + plane, left and right border are the screen (or viewport in this case), adjusted to -1 to 1
        Vector2 rayVector = dir + new Vector2(plane.X * cameraX, plane.Y * cameraX); // TODO Vector2.Multiply(plane, cameraX); ?
        int mapX = (int)player.X, mapY = (int)player.Y; // current square in the map the ray is
        double sideDistX, sideDistY; // distance from initial position to the first x/y side of the next cell
        double deltaDistX, deltaDistY; // distance to go from one x/y side to the next

        deltaDistX = (rayVector.X == 0) ? 1e30 : Math.Abs(1 / rayVector.X);
        deltaDistY = (rayVector.Y == 0) ? 1e30 : Math.Abs(1 / rayVector.Y);
        double wallHeight;

        int stepX, stepY;

        int hit = 0;
        int side = 0;

        var originParsed = new Vector2(1, -1) * player * gridWidth;
        originParsed.Y += SCREEN_HEIGHT;
        var end = originParsed + new Vector2(1, -1) * rayVector * gridWidth;
        DrawLineEx(originParsed, end, 4f, Color.Purple);

        if (rayVector.X < 0)
        {
            stepX = -1;
            sideDistX = (player.X - mapX) * deltaDistX;
        }
        else
        {
            stepX = 1;
            sideDistX = (mapX + 1.0 - player.X) * deltaDistX;
        }

        if (rayVector.Y < 0)
        {
            stepY = -1;
            sideDistY = (player.Y - mapY) * deltaDistY;
        }
        else
        {
            stepY = 1;
            sideDistY = (mapY + 1.0 - player.Y) * deltaDistY;
        }

        while (hit == 0)
        {
            if (sideDistX < sideDistY)
            {
                sideDistX += deltaDistX;
                mapX += stepX;
                side = 0;
            }
            else
            {
                sideDistY += deltaDistY;
                mapY += stepY;
                side = 1;
            }

            if (MapAt(mapX, mapY) > 0) hit = 1;
        }

        if (side == 0) wallHeight = (sideDistX - deltaDistX); else wallHeight = (sideDistY - deltaDistY);

        int lineHeight = (int)(SCREEN_HEIGHT / wallHeight);
        int wallStartPixel = -lineHeight / 2 + SCREEN_HEIGHT / 2;
        if (wallStartPixel < 0) wallStartPixel = 0;
        int wallEndPixel = lineHeight / 2 + SCREEN_HEIGHT / 2;
        if (wallEndPixel >= SCREEN_HEIGHT) wallEndPixel = SCREEN_HEIGHT;

        //Color wallColor = MapAt(mapX, mapY) switch
        //{
        //    1 => Color.Red,
        //    2 => Color.Green,
        //    3 => Color.Blue,
        //    4 => Color.White,
        //    _ => Color.Yellow
        //};

        //if (side == 1) wallColor = new Color(wallColor.R / 2, wallColor.G / 2, wallColor.B / 2);

        double wallX;
        if (side == 0) wallX = player.Y + wallHeight * rayVector.Y; else wallX = player.X + wallHeight * rayVector.X;
        wallX -= Math.Floor(wallX);
        // x coordinate on the texture
        int texW = textureWall.Width;
        int texH = textureWall.Height;

        int textX = (int)(wallX * texW);
        if (side == 0 && rayVector.X > 0) textX = texW - textX - 1;
        if (side == 1 && rayVector.Y < 0) textX = texW - textX - 1;

        double step = 1.0 * texH / lineHeight;
        double texPos = (wallStartPixel - SCREEN_HEIGHT / 2 + lineHeight / 2) * step;

        for (int i = wallStartPixel; i < wallEndPixel; i++)
        {
            int texY = Math.Min((int)texPos, texH - 1);
            texPos += step;
            var color = GetImageColor(textureWall, textX, texY);
            if (side == 1) color = new Color(color.R / 2, color.G / 2, color.B / 2);
            buffer[i * VIEWPORT + x] = color;
        }
        //Raylib.DrawLine(x + VIEWPORT, wallStartPixel, x + VIEWPORT, wallEndPixel, wallColor);
    }

    UpdateTexture(texture, buffer);
    DrawTexturePro(texture, new Rectangle(0, 0, VIEWPORT, SCREEN_HEIGHT), new Rectangle(VIEWPORT, 0, VIEWPORT, SCREEN_HEIGHT), Vector2.Zero, 0f, Color.White);

    DrawVector(player, dir, Color.Yellow);
    DrawFPS(SCREEN_WIDTH - 120, 10);
    //DrawText($"x={player.X:F3} y={player.Y:F3}", SCREEN_WIDTH - 100, 30, 12, Color.Green);
    EndDrawing();
}

Raylib.UnloadImage(textureWall);
Raylib.UnloadTexture(texture);
CloseWindow();


void DrawVector(Vector2 origin, Vector2 vector, Color color)
{
    var originParsed = new Vector2(1, -1) * origin * gridWidth;
    originParsed.Y += SCREEN_HEIGHT;
    var end = originParsed + new Vector2(1, -1) * vector * gridWidth;
    DrawLineEx(originParsed, end, 4f, color);

    var length = vector.Length();
    var unitDirection = new Vector2(vector.X / length, vector.Y / length);

    // (x,y) rotate 90 = (-y,x);
    var perpendicularVectorToDirection = new Vector2(-unitDirection.Y, unitDirection.X);

    // drawing unit direction vector
    //DrawLineEx(origin, origin + new Vector2(1, -1) * unitDirection * gridWidth, 3f, Color.Yellow);

    var leftArrow = vector - unitDirection * 0.3f;
    leftArrow += perpendicularVectorToDirection * 0.3f;
    DrawLineEx(end, originParsed + new Vector2(1, -1) * leftArrow * gridWidth, 3f, color);

    var rightArrow = vector - unitDirection * 0.3f;
    rightArrow += -1 * perpendicularVectorToDirection * 0.3f;
    DrawLineEx(end, originParsed + new Vector2(1, -1) * rightArrow * gridWidth, 3f, color);
}

bool TryMove(Vector2 delta, float radius)
{
    var moved = false;
    if (MapAt((int)(player.X + delta.X + Math.Sign(delta.X) * radius), (int)player.Y) == 0)
    {
        player.X += delta.X;
        moved = true;
    }

    if (MapAt((int)player.X, (int)(player.Y + delta.Y + Math.Sign(delta.Y) * radius)) == 0)
    {
        player.Y += delta.Y;
        moved = true;
    }

    return moved;
}

// map is stored as [row, col] with row 0 at the top; world y points up
byte MapAt(int x, int y) => map[mapHeight - 1 - y, x];