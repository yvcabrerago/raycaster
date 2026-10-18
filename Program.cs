using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;

// flags
bool shouldRenderWalls = true;
bool shouldRenderFloor = true;
bool shouldRenderFog = true;
bool shouldRenderSprites = true;
bool shouldPlayAudioEffect = true;

const int WINDOW_WIDTH = 320 * 3;
const int WINDOW_HEIGHT = 720;
const int VIEWPORT_WIDTH = 320;
const int VIEWPORT_HEIGHT = 240;

InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Raycaster");
InitAudioDevice();
DisableCursor();
//SetTargetFPS(60);
//ToggleFullscreen();

Sound sound = LoadSound("./resources/coin.wav");

int ACTUAL_SCREEN_WIDTH = GetScreenWidth();
int ACTUAL_SCREEN_HEIGHT = GetScreenHeight();

// buffer
Color[] buffer = new Color[VIEWPORT_WIDTH * VIEWPORT_HEIGHT];
Image canvasImage = GenImageColor(VIEWPORT_WIDTH, VIEWPORT_HEIGHT, Color.Red);
Texture2D texture = LoadTextureFromImage(canvasImage);
UnloadImage(canvasImage);

// TODO : add pipeline to load resource
Image textureWall = LoadImage("./resources/greystone.png");
Image textureWall2 = LoadImage("./resources/BRICK_3B.PNG");
Image textureWall3 = LoadImage("./resources/SLIME_1A.PNG");//LoadImage("./resources/WARN_1A.PNG");
Image textureWall4 = LoadImage("./resources/CRATE_2C.PNG");

Image textureSprite1 = LoadImage("./resources/barrel.png");

Image textureFloor3 = LoadImage("./resources/SLIME_1A.PNG");
Image textureFloor1 = LoadImage("./resources/FLOOR_4A.PNG");
Image textureFloor2 = LoadImage("./resources/SAND_1A.PNG");
Color[] textureColors1 = ToColorArray(textureFloor1);
Color[] textureColors2 = ToColorArray(textureFloor2);
Color[] textureColors3 = ToColorArray(textureFloor3);
int floorTexW = 64;
int floorTexH = 64;

SetTextureFilter(texture, TextureFilter.Bilinear);

const float maxDistanceFog = 7f;
var fogColor = Color.Black;

// TODO generate map using backtracking algo
const int mapWidth = 24;
const int mapHeight = 12;
byte[,] map = new byte[mapHeight, mapWidth]{
  {4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,7,7,7,7,7,7,7,7},
  {4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7,0,0,0,0,0,0,7},
  {4,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7},
  {4,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,7},
  {4,0,3,0,0,0,0,0,0,0,0,0,0,0,0,0,7,0,0,0,0,0,0,7},
  {4,0,4,0,0,0,0,5,5,5,5,5,5,5,5,5,7,7,0,7,7,7,7,7},
  {4,0,5,0,0,0,0,5,0,5,0,5,0,5,0,5,7,0,0,0,7,7,7,1},
  {4,0,6,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,0,0,0,8},
  {4,0,7,0,0,0,0,0,0,0,0,3,0,0,0,0,0,0,0,0,7,7,7,1},
  {4,0,8,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,0,0,0,8},
  {4,0,0,0,0,0,0,5,0,0,0,0,0,0,0,5,7,0,0,0,7,7,7,1},
  {4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,7,7,7,7,7,7,7,7}
};

double[] perpendicularDistanceBuffer = new double[VIEWPORT_WIDTH];
Vector2[] columnsPerRow = new Vector2[VIEWPORT_WIDTH];

var cameraYPosition = 0;
var maxCameraYPosition = VIEWPORT_HEIGHT / 4;
Vector2 player = new(1.6f, 1.5f);   // player position vector
Vector2 dir = new(-1, 0);           // player direction of view
Vector2 plane = new(0, 0.66f);      // perpendicular plane to direction of view
float angle = MathF.PI;
const float planeLen = 0.66f;
float rotationSpeed = 0.002f;
float soundDistance = 0f;
double soundAttenuation = 0f;

double textureOffsetCeiling = 0f;
float textureOffsetWall = 0f;

// sprites
Vector2[] sprites = new Vector2[2];
sprites[0] = new(20f, 10.5f);
sprites[1] = new(21f, 10.5f);

Vector2 automatedMove = dir;

while (!WindowShouldClose())
{
    Array.Clear(buffer);
    Array.Fill(buffer, Color.Red, 0, buffer.Length);
    var dt = GetFrameTime();
    double moveSpeed = dt * 3.0; // TODO extract into variable

    // update
    // TODO create console ?
    if (IsKeyPressed(KeyboardKey.One)) shouldRenderFloor = !shouldRenderFloor;
    if (IsKeyPressed(KeyboardKey.Two)) shouldRenderWalls = !shouldRenderWalls;
    if (IsKeyPressed(KeyboardKey.Three)) shouldRenderFog = !shouldRenderFog;
    if (IsKeyPressed(KeyboardKey.Four))
    {
        ToggleFullscreen();
        ACTUAL_SCREEN_WIDTH = GetScreenWidth();
        ACTUAL_SCREEN_HEIGHT = GetScreenHeight();
    }
    if (IsKeyPressed(KeyboardKey.Five)) shouldRenderSprites = !shouldRenderSprites;
    if (IsKeyPressed(KeyboardKey.Six)) shouldPlayAudioEffect = !shouldPlayAudioEffect;

    float playerMovRadius = 0.25f; // don't allow the player to come too close to the walls TODO extract outside the loop
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

    // mouse movement
    Vector2 delta = GetMouseDelta();
    angle -= delta.X * rotationSpeed;
    dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    plane = new Vector2(dir.Y, -dir.X) * planeLen;
    if (delta.Y != 0)
    {
        cameraYPosition += -(int)delta.Y;
        cameraYPosition = Math.Clamp(cameraYPosition, -maxCameraYPosition, maxCameraYPosition);
    }

    if (shouldPlayAudioEffect)
    {
        UpdateSoundPosition();
        if (!IsSoundPlaying(sound)) PlaySound(sound);
    }
    else
    {
        StopSound(sound);
    }

    textureOffsetWall += dt * 0.5f; // TODO do I want texture animation ?
    textureOffsetWall %= 1f;

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

    if (shouldRenderFloor)
    {
        RenderFloorAndCeiling(dt);
    }

    if (shouldRenderSprites)
    {
        RenderSprites();
    }

    UpdateTexture(texture, buffer);
    DrawTexturePro(texture, new Rectangle(0, 0, VIEWPORT_WIDTH, VIEWPORT_HEIGHT), new Rectangle((ACTUAL_SCREEN_WIDTH - VIEWPORT_WIDTH * 3) / 2, 0, VIEWPORT_WIDTH * 3, VIEWPORT_HEIGHT * 3), Vector2.Zero, 0f, Color.White);

    DrawFPS(ACTUAL_SCREEN_WIDTH - 120, 10);
    //DrawText($"soundDistance={soundDistance:F3} soundAttenuation={soundAttenuation:F3}", SCREEN_WIDTH - 300, 30, 12, Color.Green);
    DrawText($"x={player.X:F3} y={player.Y:F3}", ACTUAL_SCREEN_WIDTH - 100, 30, 12, Color.Green);
    EndDrawing();
}

UnloadImage(textureWall);
UnloadImage(textureWall2);
UnloadImage(textureWall3);
UnloadImage(textureWall4);
UnloadImage(textureFloor1);
UnloadImage(textureFloor2);
UnloadImage(textureFloor3);
UnloadImage(textureSprite1);
UnloadTexture(texture);
UnloadSound(sound);
CloseAudioDevice();
CloseWindow();

void RenderSprites()
{
    // TODO use built in functions for matrix projection
    sprites.Sort((Vector2 v1, Vector2 v2) =>
    {
        var d1 = Vector2.Distance(v1, player);
        var d2 = Vector2.Distance(v2, player);
        return (d1 <= d2) ? 1 : -1;
    });

    for (int i = 0; i < sprites.Length; i++)
    {
        var sprite = sprites[i] - player;

        // inverse camera matrix
        double invDet = 1f / (plane.X * dir.Y - dir.X * plane.Y);
        double transformX = invDet * (dir.Y * sprite.X - dir.X * sprite.Y);
        double transformY = invDet * (-plane.Y * sprite.X + plane.X * sprite.Y);

        int spriteScreenX = (int)((VIEWPORT_WIDTH / 2) * (1 + transformX / transformY));

        // height of sprite
        int spriteHeight = Math.Abs((int)(VIEWPORT_HEIGHT / transformY));
        int drawStartY = -spriteHeight / 2 + VIEWPORT_HEIGHT / 2 + cameraYPosition;
        if (drawStartY < 0) drawStartY = 0;
        int drawEndY = spriteHeight / 2 + VIEWPORT_HEIGHT / 2 + cameraYPosition;
        if (drawEndY > VIEWPORT_HEIGHT) drawEndY = VIEWPORT_HEIGHT - 1;

        // width of sprite
        int spriteWidth = spriteHeight; // is this assuming square sprites ?
        int drawStartX = -spriteWidth / 2 + spriteScreenX;
        if (drawStartX < 0) drawStartX = 0;
        int drawEndX = spriteWidth / 2 + spriteScreenX;
        if (drawEndX > VIEWPORT_WIDTH) drawEndX = VIEWPORT_WIDTH - 1;

        // texture mapping
        for (int stripe = drawStartX; stripe < drawEndX; stripe++)
        {
            int texX = (stripe - (-spriteWidth / 2 + spriteScreenX))
               * textureSprite1.Width / spriteWidth;

            if (transformY > 0 && stripe > 0 && stripe < VIEWPORT_WIDTH && transformY < perpendicularDistanceBuffer[stripe])
            {
                for (int y = drawStartY; y < drawEndY; y++)
                {
                    int d = (y - cameraYPosition) * 256 - (VIEWPORT_HEIGHT) * 128 + spriteHeight * 128;
                    int texY = (d * textureSprite1.Height / spriteHeight) / 256;
                    var color = GetImageColor(textureSprite1, texX, texY);
                    if (color != Color.Black)  // black as transparency color
                    {
                        if (shouldRenderFog)
                        {
                            color = ColorLerp(color, fogColor, 0.2f + MathF.Pow((float)MathF.Min((float)sprite.Length(), maxDistanceFog) / maxDistanceFog, 2));
                        }

                        buffer[VIEWPORT_WIDTH * y + stripe] = color;
                    }
                }
            }
        }
    }
}

void UpdateSoundPosition()
{
    // TODO need more realistic function and also add attenuation based on raycasting for wall inbetween
    // TODO need code for sound pan
    var maxDistance = 8f;
    Vector2 soundLocation = new Vector2(20f, 10.5f);
    var directDistance = (player - soundLocation).Length();
    soundDistance = directDistance;
    float attenuation = 1 - MathF.Pow(MathF.Min((float)directDistance, maxDistance) / maxDistance, 2);

    soundAttenuation = attenuation;
    SetSoundVolume(sound, attenuation);
}

bool TryMove(Vector2 delta, float radius)
{
    bool blocked = false;
    if (delta.X != 0)
    {
        float edgeX = player.X + delta.X + Math.Sign(delta.X) * radius;
        if (Free(edgeX, player.Y - radius) && Free(edgeX, player.Y + radius))
            player.X += delta.X;
        else
            blocked = true;
    }

    if (delta.Y != 0)
    {
        float edgeY = player.Y + delta.Y + Math.Sign(delta.Y) * radius;
        if (Free(player.X - radius, edgeY) && Free(player.X + radius, edgeY))
            player.Y += delta.Y;
        else
            blocked = true;
    }

    return !blocked;
}

void RenderFloorAndCeiling(double dt)
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
        var fogPerRow = 0.2f + MathF.Pow(MathF.Min((float)dist.Length(), maxDistanceFog) / maxDistanceFog, 2);

        Span<Color> row = buffer.AsSpan(start: y * VIEWPORT_WIDTH, length: VIEWPORT_WIDTH);
        for (int x = 0; x < row.Length; x++)
        {

            int cellX = (int)floorX;
            int cellY = (int)floorY;

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

            int tx = (int)(floorX * floorTexW) & (floorTexW - 1);   // if text are 64x64 only
            int ty = (int)(floorY * floorTexH) & (floorTexH - 1);

            // ceiling movement
            //if (textureFllor == textureColors3)
            //{
            //    textureOffsetCeiling += dt * 0.00001f;
            //    tx = (int)((floorX + textureOffsetCeiling) * floorTexW) & (floorTexW - 1);
            //    ty = (int)((floorY + textureOffsetCeiling) * floorTexH) & (floorTexH - 1);
            //}

            var color = textureFllor[ty * floorTexW + tx];

            // fog
            if (shouldRenderFog)
            {
                color = ColorLerp(color, fogColor, fogPerRow);
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
        int texW = textureWall.Width;
        int texH = textureWall.Height;

        // x coordinate on the texture
        int textX = (int)(wallX * texW);
        if (side == 0 && rayVector.X > 0) textX = texW - textX - 1;
        if (side == 1 && rayVector.Y < 0) textX = texW - textX - 1;

        // wall movement
        if (MapAt(mapX, mapY) == 3)
        {
            int offsetPixels = (int)(textureOffsetWall * texW);
            textX = (textX + offsetPixels) % texW;
        }

        double step = 1.0 * texH / lineHeight;
        double texPos = (wallStartPixel - VIEWPORT_HEIGHT / 2 + lineHeight / 2 - cameraYPosition) * step;
        float fogPerStripe = 0.2f + MathF.Pow((float)MathF.Min((float)wallDistance, maxDistanceFog) / maxDistanceFog, 2);

        for (int i = wallStartPixel; i < wallEndPixel; i++)
        {
            int texY = Math.Min((int)texPos, texH - 1);
            texPos += step;
            var color = GetImageColor(textureImage, textX, texY);
            if (side == 1) color = new Color(color.R / 2, color.G / 2, color.B / 2);

            // fog here
            if (shouldRenderFog)
            {
                color = ColorLerp(color, fogColor, fogPerStripe);
            }

            buffer[i * VIEWPORT_WIDTH + x] = color;
        }
        columnsPerRow[x] = new Vector2(wallStartPixel, wallEndPixel);
        perpendicularDistanceBuffer[x] = wallDistance;
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