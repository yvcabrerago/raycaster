using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;

// flags
bool shouldRenderWalls = true;
bool shouldRenderFloor = true;
bool shouldRenderFog = true;

const int SCREEN_WIDTH = 1024;
const int SCREEN_HEIGHT = 720;


const int VIEWPORT_WIDTH = 380;
const int VIEWPORT_HEIGHT = 240;

const int mapWidth = 24;
const int mapHeight = 24;

InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Raycaster");
InitAudioDevice();
Sound sound = LoadSound("./coin.wav");
//SetTargetFPS(60);
//ToggleFullscreen();

// buffer
Color[] buffer = new Color[VIEWPORT_WIDTH * VIEWPORT_HEIGHT];
Image image = GenImageColor(VIEWPORT_WIDTH, VIEWPORT_HEIGHT, Color.SkyBlue);
Image textureWall = LoadImage("./greystone.png");
Image textureWall2 = LoadImage("./BRICK_3B.PNG");
Image textureWall3 = LoadImage("./WARN_1A.PNG");
Image textureWall4 = LoadImage("./CRATE_2C.PNG");

Image textureFloor3 = LoadImage("./SLIME_1A.PNG");
Image textureFloor1 = LoadImage("./FLOOR_4A.PNG");
Image textureFloor2 = LoadImage("./SAND_1A.PNG");
Color[] textureColors1 = ToColorArray(textureFloor1);
Color[] textureColors2 = ToColorArray(textureFloor2);
Color[] textureColors3 = ToColorArray(textureFloor3);
int floorTexW = textureFloor2.Width;
int floorTexH = textureFloor2.Height;

Texture2D texture = LoadTextureFromImage(image);
UnloadImage(image);
SetTextureFilter(texture, TextureFilter.Point);

const float maxDistanceFog = 7f;

var fogColor = Color.Black;

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
  {4,0,6,0,6,0,0,0,0,4,6,0,0,0,0,0,4,0,0,0,0,0,0,2},
  {4,0,0,5,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,2,0,2,2},
  {4,0,6,0,6,0,0,0,0,4,6,0,6,2,0,0,3,0,0,2,0,0,0,2},
  {4,0,0,0,0,0,0,0,0,4,6,0,6,2,0,0,0,0,0,2,0,0,0,2},
  {4,4,4,4,4,4,4,4,4,4,1,1,1,2,2,2,2,2,2,3,3,3,3,3}
};

Vector2[] columnsPerRow = new Vector2[VIEWPORT_WIDTH];

var cameraYPosition = 0;
var maxCameraYPosition = VIEWPORT_HEIGHT / 4;
Vector2 player = new Vector2(1.6f, 1.5f);   // player position vector
Vector2 dir = new Vector2(-1, 0);           // player direction of view
Vector2 plane = new Vector2(0, 0.66f);      // perpendicular plane to direction of view
float angle = MathF.PI;
const float planeLen = 0.66f;
float rotationSpeed = 0.002f;
float soundDistance = 0f;
Raylib.DisableCursor();

// DEBUG vars
double soundAttenuation = 0f;

while (!WindowShouldClose())
{
    Array.Clear(buffer);
    Array.Fill(buffer, Color.Red, 0, buffer.Length);
    var dt = GetFrameTime();
    double moveSpeed = dt * 3.0;

    // update
    // TODO create console ?
    if (IsKeyPressed(KeyboardKey.One)) shouldRenderFloor = !shouldRenderFloor;
    if (IsKeyPressed(KeyboardKey.Two)) shouldRenderWalls = !shouldRenderWalls;
    if (IsKeyPressed(KeyboardKey.Three)) shouldRenderFog = !shouldRenderFog;

    float playerMovRadius = 0.25f; // don't allow the player to come too close to the walls
    Vector2 right = new Vector2(dir.Y, -dir.X);
    Vector2 toMove = Vector2.Zero;
    if (IsKeyDown(KeyboardKey.W)) { toMove += dir; }
    if (IsKeyDown(KeyboardKey.S)) { toMove -= dir; }
    if (IsKeyDown(KeyboardKey.A)) { toMove -= right; }
    if (IsKeyDown(KeyboardKey.D)) { toMove += right; }

    if (toMove != Vector2.Zero)
    {
        TryMove(Vector2.Normalize(toMove) * (float)moveSpeed, playerMovRadius);
    }

    Vector2 delta = Raylib.GetMouseDelta();

    angle -= delta.X * rotationSpeed;
    dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    plane = new Vector2(dir.Y, -dir.X) * planeLen;

    //if (IsKeyDown(KeyboardKey.Up))
    if (delta.Y != 0)
    {
        cameraYPosition += -(int)delta.Y;
        cameraYPosition = Math.Clamp(cameraYPosition, -maxCameraYPosition, maxCameraYPosition);
    }

    UpdateSoundPosition();
    if (!IsSoundPlaying(sound)) PlaySound(sound);

    // drawing
    BeginDrawing();
    ClearBackground(new Color(0x0C, 0x0C, 0x0C));

    if (shouldRenderWalls)
    {
        RenderWalls();
    }
    else
    {
        Array.Clear(columnsPerRow);
    }

    // raycasting
    if (shouldRenderFloor)
    {
        RenderFloorAndCeiling();
    }

    UpdateTexture(texture, buffer);
    DrawTexturePro(texture, new Rectangle(0, 0, VIEWPORT_WIDTH, VIEWPORT_HEIGHT), new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), Vector2.Zero, 0f, Color.White);

    //DrawVector(player, dir, Color.Yellow);
    DrawFPS(SCREEN_WIDTH - 120, 10);
    DrawText($"soundDistance={soundDistance:F3} soundAttenuation={soundAttenuation:F3}", SCREEN_WIDTH - 300, 30, 12, Color.Green);
    //DrawText($"x={player.X:F3} y={player.Y:F3}", SCREEN_WIDTH - 100, 30, 12, Color.Green);
    EndDrawing();
}

Raylib.UnloadImage(textureWall);
Raylib.UnloadImage(textureWall2);
Raylib.UnloadImage(textureWall3);
Raylib.UnloadImage(textureWall4);
Raylib.UnloadImage(textureFloor1);
Raylib.UnloadImage(textureFloor2);
Raylib.UnloadTexture(texture);
UnloadSound(sound);
CloseAudioDevice();
CloseWindow();


void UpdateSoundPosition()
{
    // need more realistic function and also add attenuation based on raycasting for wall inbetween
    // need code for sound pan
    var maxDistance = 8f;
    Vector2 soundLocation = new Vector2(20,20);
    var directDistance = (player - soundLocation).Length();
    soundDistance = directDistance;
    float attenuation = 1-MathF.Pow(MathF.Min((float)directDistance, maxDistance) / maxDistance, 2);

    soundAttenuation = attenuation;
    SetSoundVolume(sound, attenuation);
}

bool TryMove(Vector2 delta, float radius)
{
    var moved = false;
    float edgeX = player.X + delta.X + Math.Sign(delta.X) * radius;
    if (Free(edgeX, player.Y - radius) && Free(edgeX, player.Y + radius))
    {
        player.X += delta.X;
        moved = true;
    }

    float edgeY = player.Y + delta.Y + Math.Sign(delta.Y) * radius;
    if (Free(player.X - radius, edgeY) && Free(player.X + radius, edgeY))
    {
        player.Y += delta.Y;
        moved = true;
    }

    return moved;
}

void RenderFloorAndCeiling()
{
    // floorcasting
    int horizon = VIEWPORT_HEIGHT / 2 + cameraYPosition;
    float posZ = VIEWPORT_HEIGHT / 2f;
    var rayDir0 = dir - plane;
    var rayDir1 = dir + plane;
    for (int y = 0; y < VIEWPORT_HEIGHT; y++)
    {
        if (y == horizon) continue;

        int p = Math.Abs(y - horizon);

        float rowDistance = posZ / p;

        float floorStepX =
            rowDistance * (rayDir1.X - rayDir0.X) / VIEWPORT_WIDTH;

        float floorStepY =
            rowDistance * (rayDir1.Y - rayDir0.Y) / VIEWPORT_WIDTH;

        float floorX = player.X + rowDistance * rayDir0.X;
        float floorY = player.Y + rowDistance * rayDir0.Y;

        Vector2 dist = new Vector2(floorX, floorY) - player;

        Span <Color> row = buffer.AsSpan(start: y * VIEWPORT_WIDTH, length: VIEWPORT_WIDTH);
        for (int x = 0; x < row.Length; x++)
        {

            int cellX = (int)floorX;
            int cellY = (int)floorY;

            //int tx = (int)(floorTexW * (floorX - cellX)) % floorTexW;
            //int ty = (int)(floorTexH * (floorY - cellY)) % floorTexH;
            int tx = (int)(floorX * floorTexW) & (floorTexW - 1);   // if text are 64x64 only
            int ty = (int)(floorY * floorTexH) & (floorTexH - 1);

            floorX += floorStepX;
            floorY += floorStepY;

            if (y >= columnsPerRow[x].X && y < columnsPerRow[x].Y)
            {
                continue;
            }

            var textureFllor = y < horizon ? textureColors3 : textureColors2;

            if (y > horizon)
            {
                if ((cellX + cellY) % 2 == 0)
                    textureFllor = textureColors1;

            }

            var color = textureFllor[ty * floorTexW + tx];

            // fog
            if (shouldRenderFog)
            {
                color = ColorLerp(color, fogColor, 0.2f + MathF.Pow(MathF.Min((float)dist.Length(), maxDistanceFog) / maxDistanceFog, 2));
            }

            row[x] = y > horizon ? new Color(color.R / 2, color.G / 2, color.B / 2) : color;


        }
    }
}


void RenderWalls()
{
    // wallcasting
    // ray starts at player
    for (int x = 0; x < VIEWPORT_WIDTH; x++)
    {
        float cameraX = 2 * (float)x / VIEWPORT_WIDTH - 1;    // camera space = pos + dir - plane to pos + dir + plane, left and right border are the screen (or viewport in this case), adjusted to -1 to 1
        Vector2 rayVector = dir + new Vector2(plane.X * cameraX, plane.Y * cameraX); // TODO Vector2.Multiply(plane, cameraX); ?
        int mapX = (int)player.X, mapY = (int)player.Y; // current square in the map the ray is
        double sideDistX, sideDistY; // distance from initial position to the first x/y side of the next cell
        double deltaDistX, deltaDistY; // distance to go from one x/y side to the next

        deltaDistX = (rayVector.X == 0) ? 1e30 : Math.Abs(1 / rayVector.X);
        deltaDistY = (rayVector.Y == 0) ? 1e30 : Math.Abs(1 / rayVector.Y);
        double wallDistance;

        int stepX, stepY;

        int hit = 0;
        int side = 0;

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

        // dda
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

        if (side == 0) wallDistance = (sideDistX - deltaDistX); else wallDistance = (sideDistY - deltaDistY);

        int lineHeight = (int)(VIEWPORT_HEIGHT / wallDistance);
        int wallStartPixel = -lineHeight / 2 + VIEWPORT_HEIGHT / 2 + cameraYPosition;
        if (wallStartPixel < 0) wallStartPixel = 0;
        int wallEndPixel = lineHeight / 2 + VIEWPORT_HEIGHT / 2 + cameraYPosition;
        if (wallEndPixel >= VIEWPORT_HEIGHT) wallEndPixel = VIEWPORT_HEIGHT;

        var textureImage = MapAt(mapX, mapY) switch
        {
            1 => textureWall,
            2 => textureWall2,
            3 => textureWall3,
            4 => textureWall4,
            _ => textureWall2
        };

        double wallX;
        if (side == 0) wallX = player.Y + wallDistance * rayVector.Y; else wallX = player.X + wallDistance * rayVector.X;
        wallX -= Math.Floor(wallX);
        // x coordinate on the texture
        int texW = textureWall.Width;
        int texH = textureWall.Height;

        int textX = (int)(wallX * texW);
        if (side == 0 && rayVector.X > 0) textX = texW - textX - 1;
        if (side == 1 && rayVector.Y < 0) textX = texW - textX - 1;

        double step = 1.0 * texH / lineHeight;
        double texPos = (wallStartPixel - VIEWPORT_HEIGHT / 2 + lineHeight / 2 - cameraYPosition) * step;

        for (int i = wallStartPixel; i < wallEndPixel; i++)
        {
            int texY = Math.Min((int)texPos, texH - 1);
            texPos += step;
            var color = GetImageColor(textureImage, textX, texY);
            if (side == 1) color = new Color(color.R / 2, color.G / 2, color.B / 2);

            // fog here
            if (shouldRenderFog)
            {
                color = ColorLerp(color, fogColor, 0.2f + MathF.Pow((float)MathF.Min((float)wallDistance, maxDistanceFog) / maxDistanceFog, 2));
            }

            buffer[i * VIEWPORT_WIDTH + x] = color;
        }
        columnsPerRow[x] = new Vector2(wallStartPixel, wallEndPixel);
    }
}

bool Free(float x, float y) => MapAt((int)x, (int)y) == 0;

// map is stored as [row, col] with row 0 at the top, y points up
byte MapAt(int x, int y) => map[mapHeight - 1 - y, x];

Color[] ToColorArray(Image img)
{
    var colors = new Color[img.Width * img.Height];
    for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
            colors[y * img.Width + x] = GetImageColor(img, x, y);
    return colors;
}