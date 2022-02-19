using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIRT;
using DIRT.Types;

namespace ConsoleCraft
{
    
    class Pig
    {
        static Random rnd = new Random();

        public Mesh pigMesh = new Mesh(Vector.zero, Vector.zero);
        public float rot = 0;
        public float targetRot = (float)rnd.NextDouble() * 1000f;
        public bool moving = false;

        

        public Pig(Vector pos)
        {
            pigMesh.makeCubeTexturedOffset(new Vector(1, 0.5f, 0.5f), 160.1f, 0.1f, 175.9f, 15.9f,Vector.zero);
            pigMesh.makeCubeTexturedOffset(new Vector(0.4f, 0.4f, 0.4f), 160.1f, 0.1f, 175.9f, 15.9f, new Vector(0.5f, -0.1f, 0));
            pigMesh.makeCubeTexturedOffset(new Vector(0.39f, 0.39f, 0.39f), 144.1f, 0.1f, 159.9f, 15.9f, new Vector(0.525f,-0.1f,0));

            pigMesh.makeCubeTexturedOffset(new Vector(0.2f, 0.4f, 0.2f), 160.1f, 0.1f, 175.9f, 15.9f, new Vector(0.4f, 0.3f, 0.149f));
            pigMesh.makeCubeTexturedOffset(new Vector(0.2f, 0.4f, 0.2f), 160.1f, 0.1f, 175.9f, 15.9f, new Vector(-0.4f, 0.3f, 0.149f));
            pigMesh.makeCubeTexturedOffset(new Vector(0.2f, 0.4f, 0.2f), 160.1f, 0.1f, 175.9f, 15.9f, new Vector(0.4f, 0.3f, -0.149f));
            pigMesh.makeCubeTexturedOffset(new Vector(0.2f, 0.4f, 0.2f), 160.1f, 0.1f, 175.9f, 15.9f, new Vector(-0.4f, 0.3f, -0.149f));

            pigMesh.position = pos;

            ConsoleRenderer.Meshes.Add(pigMesh);
        }

        public void step()
        {
            if (rnd.Next(0, 100) < 1)
                moving = true;

            if (rnd.Next(0, 100) < 2)
                moving = false;

            if (rnd.Next(0, 100) < 1)
                targetRot += (((float)rnd.NextDouble()) - 0.5f) * 3f;
        }

        
    }
}
