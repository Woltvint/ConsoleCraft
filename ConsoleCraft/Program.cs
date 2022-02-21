using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

using SimplexNoise;

using DIRT;
using DIRT.Types;

namespace ConsoleCraft
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pos);

        [DllImport("user32.dll")]
        public static extern long GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        static (Mesh, int, bool)[,,] map;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to ConsoleCraft");

            Thread.Sleep(2000);

            float targetRenderDist = 20;

            if (!File.Exists("options.conf"))
            {
                File.WriteAllText("options.conf", JsonSerializer.Serialize(new Options(), new JsonSerializerOptions() { WriteIndented = true }));
            }

            Options opt = JsonSerializer.Deserialize<Options>(File.ReadAllText("options.conf"));

            //set up the dirt console
            ConsoleSettings.screenAutoSize = opt.windowAutoSize;
            ConsoleSettings.renderDistance = 0.01f;
            ConsoleSettings.screenMode = ConsoleSettings.screenModes.graySpeed;
            ConsoleSettings.backgroundColor = new Vector(135, 206, 235);

            if (opt.windowAutoSize)
            {
                ConsoleSettings.screenWidth = opt.screenWidth;
                ConsoleSettings.screenHeight = opt.screenHeight;
            }

            ConsoleRenderer.startRenderer();

            ConsoleRenderer.textureMap = (Bitmap)Image.FromFile("textureMap.png");

            map = new (Mesh, int, bool)[opt.mapSize, 20, opt.mapSize];

            targetRenderDist = opt.renderDistance;

            Random rnd = new Random();
            List<Pig> pigs = new List<Pig>();
            Mesh cross = new Mesh(Vector.zero, Vector.zero);

            lock (ConsoleRenderer.renderLock)
            {
                Vector light = new Vector(-0.5f, -0.8f, -1f);
                ConsoleRenderer.Lights.Add(light);

                for (int x = 0; x < map.GetLength(0); x++)
                {
                    for (int z = 0; z < map.GetLength(2); z++)
                    {
                        int heightNoiseStone = (int)((Noise.CalcPixel2D(x, z, 0.01f) / 255f) * 10);
                        int heightNoiseGrass = (int)((Noise.CalcPixel2D(x + 1000, z + 1000, 0.01f) / 255f) * 20);

                        for (int y = 0; y < map.GetLength(1); y++)
                        {
                            
                            if (y > 4)
                            {
                                if (heightNoiseGrass > heightNoiseStone)
                                {
                                    if (y < heightNoiseStone)
                                    {
                                        map[x, y, z].Item2 = 1;
                                    }
                                    else
                                    {
                                        if (y < heightNoiseGrass)
                                            map[x, y, z].Item2 = 2;
                                        else if (y == heightNoiseGrass)
                                            map[x, y, z].Item2 = 3;
                                    }
                                }
                                else
                                {
                                    if (y <= heightNoiseStone)
                                    {
                                        map[x, y, z].Item2 = 1;
                                    }
                                }
                            }
                            else
                            {
                                if (y <= heightNoiseStone)
                                {
                                    map[x, y, z].Item2 = 1;
                                }
                                else
                                {
                                    map[x, y, z].Item2 = 6;
                                }
                            }

                            

                            map[x, y, z].Item1 = new Mesh(new Vector(x, -y, z), Vector.zero);

                            map[x, y, z].Item3 = true;

                            ConsoleRenderer.Meshes.Add(map[x, y, z].Item1);
                        }
                    }
                }

                for (int i = 0; i < opt.trees; i++)
                {
                    int px = rnd.Next(3, map.GetLength(0)-3);
                    int pz = rnd.Next(3, map.GetLength(2) - 3);

                    int py = map.GetLength(1) - 2;

                    for (;;py--)
                    {
                        if (isBlock(new Vector(px,py,pz)))
                        {
                            break;
                        }
                    }

                    if (py < 0 || py >= map.GetLength(1))
                    {
                        i--;
                        continue;
                    }

                    if (map[px,py,pz].Item2 == 3)
                    {
                        py += 1;

                        for (int j = 0; j < 4; j++)
                        {
                            map[px, py, pz].Item2 = 4;

                            py++;
                        }

                        map[px, py, pz].Item2 = 5;

                        map[px+1, py, pz].Item2 = 5;
                        map[px-1, py, pz].Item2 = 5;
                        map[px, py, pz+1].Item2 = 5;
                        map[px, py, pz-1].Item2 = 5;

                        map[px + 1, py-1, pz].Item2 = 5;
                        map[px - 1, py-1, pz].Item2 = 5;
                        map[px, py-1, pz + 1].Item2 = 5;
                        map[px, py-1, pz - 1].Item2 = 5;

                        map[px + 1, py - 1, pz-1].Item2 = 5;
                        map[px - 1, py - 1, pz+1].Item2 = 5;
                        map[px+1, py - 1, pz + 1].Item2 = 5;
                        map[px-1, py - 1, pz - 1].Item2 = 5;

                        if (rnd.Next(0,100) < 50)
                        {
                            map[px + 2, py - 1, pz].Item2 = 5;
                            map[px - 2, py - 1, pz].Item2 = 5;
                            map[px, py - 1, pz + 2].Item2 = 5;
                            map[px, py - 1, pz - 2].Item2 = 5;
                        }
                        
                    }
                    else
                    {
                        i--;
                        continue;
                    }
                }

                for (int i = 0; i < opt.pigs; i++)
                {
                    Pig p = new Pig(new Vector(rnd.Next(2, map.GetLength(0) - 2), 20, rnd.Next(2, map.GetLength(2) - 2)));
                    pigs.Add(p);
                }

                ConsoleSettings.camera = new Vector(5, -10, 5);

                cross.makeCube(0.025f, 0.0025f, 0.001f);
                cross.makeCube(0.0025f, 0.025f, 0.001f);
                ConsoleRenderer.Meshes.Add(cross);
            }

            Rectangle sc = new Rectangle();
            GetWindowRect(GetConsoleWindow(), ref sc);
            SetCursorPos((sc.Width / 2) + sc.Left, (sc.Height / 2) + sc.Top);
            Point mp = new Point();
            GetCursorPos(out mp);

            bool capture = false;

            while (true)
            {
                Thread.Sleep(16);
                //ConsoleSettings.renderDistance += 0.01f;

                lock (ConsoleRenderer.renderLock)
                {
                    //DIRT.Lights[0] = new Vector(MathF.Cos(rot), -0.25f, MathF.Sin(rot));

                    for (int x = 0; x < map.GetLength(0); x++)
                    {
                        for (int y = 0; y < map.GetLength(1); y++)
                        {
                            for (int z = 0; z < map.GetLength(2); z++)
                            {
                                if (map[x, y, z].Item3)
                                {
                                    map[x, y, z].Item1.tris.Clear();

                                    /*if (!(isBlock(new Vector(x - 1, y, z)) && isBlock(new Vector(x + 1, y, z)) && isBlock(new Vector(x, y - 1, z)) &&
                                        isBlock(new Vector(x, y, z - 1)) && isBlock(new Vector(x, y, z + 1)) && isBlock(new Vector(x, y + 1, z))))
                                    {
                                        if (map[x, y, z].Item2 > 0)
                                            map[x, y, z].Item1.makeCubeTextured(1, 1, 1, (map[x, y, z].Item2 - 1) * 16 + 0.1f, 0.1f, map[x, y, z].Item2 * 16 - 0.1f, 15.9f);

                                    }*/

                                    if (map[x, y, z].Item2 == 0)
                                        continue;

                                    float sizeX = 1, sizeY = 1, sizeZ = 1;
                                    float texSX = (map[x, y, z].Item2 - 1) * 16 + 0.1f, texSY = 0.1f, texEX = map[x, y, z].Item2 * 16 - 0.1f, texEY = 15.9f;

                                    if (!isBlock(new Vector(x, y, z - 1)))
                                    {
                                        //front
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), -(sizeY / 2), -(sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texSX, texEY), new Vector(texEX, texEY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), -(sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), -(sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texEX, texEY), new Vector(texEX, texSY)
                                        ));
                                    }

                                    if (!isBlock(new Vector(x, y, z + 1)))
                                    {
                                        //back
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), (sizeZ / 2))
                                            , new Vector(texEX, texSY), new Vector(texEX, texEY), new Vector(texSX, texEY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), -(sizeY / 2), (sizeZ / 2))
                                            , new Vector(texEX, texSY), new Vector(texSX, texEY), new Vector(texSX, texSY)
                                        ));
                                    }

                                    if (!isBlock(new Vector(x + 1, y, z)))
                                    {
                                        //left
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), (sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texEX, texSY), new Vector(texEX, texEY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector((sizeX / 2), -(sizeY / 2), (sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texEX, texEY), new Vector(texSX, texEY)
                                        ));
                                    }

                                    if (!isBlock(new Vector(x - 1, y, z)))
                                    {
                                        //right
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texSX, texEY), new Vector(texEX, texEY), new Vector(texEX, texSY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector(-(sizeX / 2), -(sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texSX, texEY), new Vector(texEX, texSY), new Vector(texSX, texSY)
                                        ));
                                    }

                                    if (!isBlock(new Vector(x, y - 1, z)))
                                    {
                                        //top
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector(-(sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), (sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texSX, texEY), new Vector(texEX, texEY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector(-(sizeX / 2), (sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), (sizeZ / 2)), new Vector((sizeX / 2), (sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texSX, texSY), new Vector(texEX, texEY), new Vector(texEX, texSY)
                                        ));
                                    }

                                    if (!isBlock(new Vector(x, y + 1, z)))
                                    {
                                        //bottom
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), -(sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texEX, texEY), new Vector(texSX, texEY), new Vector(texSX, texSY)
                                        ));
                                        map[x, y, z].Item1.tris.Add(new Triangle(new Vector((sizeX / 2), -(sizeY / 2), (sizeZ / 2)), new Vector(-(sizeX / 2), -(sizeY / 2), -(sizeZ / 2)), new Vector((sizeX / 2), -(sizeY / 2), -(sizeZ / 2))
                                            , new Vector(texEX, texEY), new Vector(texSX, texSY), new Vector(texEX, texSY)
                                        ));
                                    }

                                    map[x, y, z].Item3 = false;
                                }
                            }
                        }
                    }


                    if (capture)
                    {
                        GetCursorPos(out mp);

                        int x = (sc.Width / 2) + sc.Left - mp.X;
                        int y = (sc.Height / 2) + sc.Top - mp.Y;
                        ConsoleSettings.cameraRot.y += (float)x / 1500f;
                        ConsoleSettings.cameraRot.x += (float)y / 1500f;

                        SetCursorPos((sc.Width / 2) + sc.Left, (sc.Height / 2) + sc.Top);
                    }

                    if (ConsoleRenderer.keyDown(ConsoleKey.Escape))
                    {
                        capture = !capture;
                    }


                    if (ConsoleRenderer.keyPressed(ConsoleKey.W))
                    {
                        ConsoleSettings.camera += new Vector(MathF.Sin(-ConsoleSettings.cameraRot.y), 0, MathF.Cos(-ConsoleSettings.cameraRot.y)) / 10f;
                    }
                    if (ConsoleRenderer.keyPressed(ConsoleKey.S))
                    {
                        ConsoleSettings.camera -= new Vector(MathF.Sin(-ConsoleSettings.cameraRot.y), 0, MathF.Cos(-ConsoleSettings.cameraRot.y)) / 10f;
                    }

                    if (ConsoleRenderer.keyPressed(ConsoleKey.D))
                    {
                        ConsoleSettings.camera += new Vector(MathF.Sin(-ConsoleSettings.cameraRot.y + (MathF.PI / 2)), 0, MathF.Cos(-ConsoleSettings.cameraRot.y + (MathF.PI / 2))) / 10f;
                    }
                    if (ConsoleRenderer.keyPressed(ConsoleKey.A))
                    {
                        ConsoleSettings.camera -= new Vector(MathF.Sin(-ConsoleSettings.cameraRot.y + (MathF.PI / 2)), 0, MathF.Cos(-ConsoleSettings.cameraRot.y + (MathF.PI / 2))) / 10f;
                    }

                    


                    if (!isBlock(new Vector(ConsoleSettings.camera.x,-ConsoleSettings.camera.y - 2f, ConsoleSettings.camera.z)))
                    {
                        ConsoleSettings.camera += new Vector(0, 0.1f, 0);
                    }
                    else if (isBlock(new Vector(ConsoleSettings.camera.x, -ConsoleSettings.camera.y-1.9f, ConsoleSettings.camera.z)))
                    {
                        ConsoleSettings.camera += new Vector(0, -0.1f, 0);
                    }

                    if (ConsoleRenderer.keyPressed(ConsoleKey.Add))
                    {
                        targetRenderDist += 0.1f;
                    }
                    if (ConsoleRenderer.keyPressed(ConsoleKey.Subtract))
                    {
                        targetRenderDist -= 0.1f;
                    }

                    ConsoleSettings.renderDistance = lerp(ConsoleSettings.renderDistance, targetRenderDist, 0.1f);


                    if (ConsoleRenderer.keyPressed(ConsoleKey.NumPad1))
                    {
                        ConsoleSettings.screenMode = ConsoleSettings.screenModes.trueColor;
                    }
                    if (ConsoleRenderer.keyPressed(ConsoleKey.NumPad2))
                    {
                        ConsoleSettings.screenMode = ConsoleSettings.screenModes.graySpeed;
                    }

                    if (ConsoleRenderer.keyPressed(ConsoleKey.NumPad4))
                    {
                        ConsoleRenderer.textureMap = (Bitmap)Image.FromFile("textureMap.png");
                    }
                    if (ConsoleRenderer.keyPressed(ConsoleKey.NumPad5))
                    {
                        ConsoleRenderer.textureMap = (Bitmap)Image.FromFile("textureMap2.png");
                    }

                    


                    if (ConsoleRenderer.keyDown(ConsoleKey.E))
                    {
                        Vector bl = getClicked(true);
                        setBlock(bl, 9);
                    }
                    if (ConsoleRenderer.keyDown(ConsoleKey.Q))
                    {
                        Vector bl = getClicked();
                        setBlock(bl, 0);
                    }


                    cross.position = ConsoleSettings.camera;
                    
                    cross.position += Vector.rotate(new Vector(0, 0, 1), ConsoleSettings.cameraRot)/2;

                    cross.rotation = ConsoleSettings.cameraRot;


                    for (int i = 0; i < pigs.Count; i++)
                    {
                        if (!isBlock(new Vector(pigs[i].pigMesh.position.x, -pigs[i].pigMesh.position.y - 0.6f, pigs[i].pigMesh.position.z)))
                        {
                            pigs[i].pigMesh.position += new Vector(0, 0.1f, 0);
                        }
                        else if (isBlock(new Vector(pigs[i].pigMesh.position.x, -pigs[i].pigMesh.position.y - 0.5f, pigs[i].pigMesh.position.z)))
                        {
                            pigs[i].pigMesh.position += new Vector(0, -0.1f, 0);
                        }

                        pigs[i].step();

                        pigs[i].rot = lerp(pigs[i].rot, pigs[i].targetRot, 0.05f);
                        pigs[i].pigMesh.rotation.y = pigs[i].rot;

                        if (pigs[i].moving)
                        {
                            pigs[i].pigMesh.position += new Vector(MathF.Cos(pigs[i].rot), 0, MathF.Sin(pigs[i].rot)) / 25f;
                        }
                    }

                }
            }
        }

        public static bool isBlock(Vector pos)
        {
            pos = new Vector(MathF.Round(pos.x), MathF.Round(pos.y), MathF.Round(pos.z));

            if (pos.x < 0 || pos.x >= map.GetLength(0))
                return true;
            if (pos.y < 0 || pos.y >= map.GetLength(1))
                return true;
            if (pos.z < 0 || pos.z >= map.GetLength(2))
                return true;

            return map[(int)pos.x, (int)pos.y, (int)pos.z].Item2 > 0;
        }

        public static Vector getClicked(bool build = false)
        {
            Vector c = new Vector(ConsoleSettings.camera.x, -ConsoleSettings.camera.y, ConsoleSettings.camera.z);

            Vector dir = Vector.rotate(new Vector(0, 0, 1), ConsoleSettings.cameraRot) / 4;

            dir.y *= -1;

            for (int i = 0; i < 20; i++)
            {
                c += dir;

                if (isBlock(c))
                {
                    if (build)
                    {
                        c -= dir;
                    }

                    break;
                }
                    
            }

            return c;
        }

        public static void setBlock(Vector pos, int blockID)
        {
            pos = new Vector(MathF.Round(pos.x), MathF.Round(pos.y), MathF.Round(pos.z));
            if (pos.x < 0 || pos.x >= map.GetLength(0))
                return;
            if (pos.y < 0 || pos.y >= map.GetLength(1))
                return;
            if (pos.z < 0 || pos.z >= map.GetLength(2))
                return;

            map[(int)pos.x, (int)pos.y, (int)pos.z].Item2 = blockID;
            updateBlock(pos);

            updateBlock(pos + new Vector(1, 0, 0));
            updateBlock(pos + new Vector(-1, 0, 0));

            updateBlock(pos + new Vector(0, 1, 0));
            updateBlock(pos + new Vector(0, -1, 0));

            updateBlock(pos + new Vector(0, 0, 1));
            updateBlock(pos + new Vector(0, 0, -1));
        }

        public static void updateBlock(Vector pos)
        {
            pos = new Vector(MathF.Round(pos.x), MathF.Round(pos.y), MathF.Round(pos.z));
            if (pos.x < 0 || pos.x >= map.GetLength(0))
                return;
            if (pos.y < 0 || pos.y >= map.GetLength(1))
                return;
            if (pos.z < 0 || pos.z >= map.GetLength(2))
                return;

            map[(int)pos.x, (int)pos.y, (int)pos.z].Item3 = true;
        }

        public static float lerp(float x, float y, float a)
        {
            return x * (1.0f - a) + y * a;
        }
    }
}
