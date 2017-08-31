using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TerrainGeneratorTutorial0._1
{
    public partial class Form1 : Form
    {
        private Device device = null;
        private VertexBuffer vb = null;
        private IndexBuffer ib = null;

        private static int terWidth = 64;
        private static int terLength = 64;
        private float moveSpeed = 0.2f;
        private float turnSpeed = 0.02f;
        private float rotY = 0; // between 0 and 2*Math.PI (=360°)
        private float tempY = 0;

        private const float raiseConst = 0.05f;

        private float rotXZ = 0;
        private float tempXZ = 0;

        private static int vertCount = terWidth * terLength; //amount of vertices
        private static int indCount = (terWidth - 1) * (terLength - 1) * 6; //amount of indices

        private Vector3 camPosition, camLookAt, camUp;

        CustomVertex.PositionColored[] verts = null; //our vertices here can only store position and color information

        bool isMiddleMouseDown = false;
        bool isLeftMouseDown = false;


        private static int[] indices = null;

        private FillMode fillMode = FillMode.WireFrame;
        private Color backgroundColor = Color.Black;

        private bool invalidating = true;

        private Bitmap heightmap = null;

        public Form1()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true); //this needs to be set so the invalidate() function in the paint event works properly
            InitializeComponent();

            InitializeGraphics();
            InitializeEventHandler();
        }
        private void InitializeGraphics()
        {
            PresentParameters pp = new PresentParameters();
            pp.Windowed = true;
            pp.SwapEffect = SwapEffect.Discard; //just how the stuff should be moved from the backbuffer to the front buffer
            pp.EnableAutoDepthStencil = true;
            pp.AutoDepthStencilFormat = DepthFormat.D16;

            device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, pp);

            GenerateVertex();
            GenerateIndex();

            vb = new VertexBuffer(typeof(CustomVertex.PositionColored), vertCount, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColored.Format, Pool.Default);
            OnVertexBufferCreate(vb, null);

            ib = new IndexBuffer(typeof(int), indCount, device, Usage.WriteOnly, Pool.Default);
            OnIndexBufferCreate(ib, null);

            //Initial Camera position
            camPosition = new Vector3(terLength/2, 4.5f, -3.5f);
            camUp = new Vector3(0, 1, 0); // the cameras up is looking actually up on the Y axis e.g. (0,-1,0) would turn the camera upside down
        }
        private void InitializeEventHandler()
        {
            vb.Created += new EventHandler(OnVertexBufferCreate);
            ib.Created += new EventHandler(OnIndexBufferCreate);

            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.MouseWheel += new MouseEventHandler(OnMouseScroll);

            this.MouseMove += new MouseEventHandler(OnMouseMove);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.MouseUp += new MouseEventHandler(OnMouseUp);

        }

        private void OnIndexBufferCreate(object sender, EventArgs e)
        {
            IndexBuffer buffer = (IndexBuffer)sender;
            buffer.SetData(indices, 0, LockFlags.None); //puts all indices from the int array into the index buffer
        }
        private void OnVertexBufferCreate(object sender, EventArgs e)
        {
            VertexBuffer buffer = (VertexBuffer)sender;
            buffer.SetData(verts, 0, LockFlags.None); //puts all vertices from the vertex array into the vertex buffer
        }
        private void SetupCamera()
        {
            camLookAt.X = (float)Math.Sin(rotY) + camPosition.X + (float)(Math.Sin(rotXZ) * Math.Sin(rotY));    //      
            camLookAt.Y =  (float) Math.Sin(rotXZ) + camPosition.Y;  // Bind the camera lookAt somehow with the camera position, so once we move around we also move the lookAt
            camLookAt.Z = (float)Math.Cos(rotY) + camPosition.Z + (float)(Math.Sin(rotXZ) * Math.Cos(rotY));  //


            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, this.Width / this.Height, 1.0f, 1000.0f);  //sets the perspective and the field of view of the camer
            device.Transform.View = Matrix.LookAtLH(camPosition, camLookAt, camUp); //sets the position, the lookat and the up vector of the camera

            device.RenderState.Lighting = false;
            device.RenderState.CullMode = Cull.CounterClockwise; //sets which site of the triangles should be shown
            device.RenderState.FillMode = fillMode; //this sets the mode so we can actually see the triangle construct of the terrain
        }
        private void Form1_Paint(object sender, PaintEventArgs e) //here is the drawing loop
        {
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, backgroundColor, 1, 0);

            SetupCamera();

            device.BeginScene();

            device.VertexFormat = CustomVertex.PositionColored.Format;

            device.SetStreamSource(0, vb, 0); //tells the device where to receive the vertex information from
            device.Indices = ib; //tells the device where to receive the index information from

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertCount, 0, indCount / 3); //draws the triangles depending on the style we set it using the information from indices

            device.EndScene();
            device.Present();

            menuStrip1.Update();

            if (invalidating)
            {
                this.Invalidate();//this makes the form redraw itself over and over so that we have some kind of a loop 
            }
        }
        private void GenerateVertex()
        {
            verts = new CustomVertex.PositionColored[vertCount];

            int k = 0;
            for (int z = 0; z < terWidth; z++)
            {
                for (int x = 0; x < terLength; x++)
                {
                    verts[k].Position = new Vector3(x, 0, z);
                    verts[k].Color = Color.White.ToArgb();
                    k++;
                }
            }

        }
        private void GenerateIndex() // Generating all the indices vor the Index buffer (Her we tell how to connect the vertices to creat triangles)
        {
            indices = new int[indCount];

            int k = 0;
            int l = 0;
            for (int i = 0; i < indCount; i += 6)
            {
                indices[i] = k;                     //
                indices[i + 1] = k + terLength;     // left triangle of the square
                indices[i + 2] = k + terLength + 1; //

                indices[i + 3] = k;                   //
                indices[i + 4] = k + terLength + 1;   // right triangle of the square
                indices[i + 5] = k + 1;               //
                k++;
                l++;
                if (l == terLength - 1) //once one line of rectangles is created and the end of the line is beeing reached skip one k
                {
                    l = 0;
                    k++;
                }
            }
        }

        private void LoadHeightmap()
        {
            verts = new CustomVertex.PositionColored[vertCount];

            int k = 0;

            using(OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Load Heightmap";
                ofd.Filter = "Bitmap files (*.bmp)|*.bmp";
                ofd.InitialDirectory = Application.StartupPath;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    heightmap = new Bitmap(ofd.FileName);
                    Color pixelCol;

                    for (int z = 0; z < terWidth; z++)
                    {
                        for (int x = 0; x < terLength; x++)
                        {
                            if(heightmap.Size.Width>x && heightmap.Size.Height > z)
                            {
                                pixelCol = heightmap.GetPixel(x, z);
                                verts[k].Position = new Vector3(x, (float)pixelCol.B / 15 - 10, z);
                                verts[k].Color = pixelCol.ToArgb();
                            }
                            else
                            {
                                verts[k].Position = new Vector3(x, 0, z);
                                verts[k].Color = Color.White.ToArgb();
                            }

                            k++;
                        }
                    }

                }
            }



        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case (Keys.W):
                    {
                        camPosition.X += moveSpeed * (float)Math.Sin(rotY);
                        camPosition.Z += moveSpeed * (float)Math.Cos(rotY);
                        break;
                    }
                case (Keys.S):
                    {
                        camPosition.X -= moveSpeed * (float)Math.Sin(rotY);
                        camPosition.Z -= moveSpeed * (float)Math.Cos(rotY);
                        break;
                    }
                case (Keys.D):
                    {
                        camPosition.X += moveSpeed * (float)Math.Sin(rotY + Math.PI / 2);
                        camPosition.Z += moveSpeed * (float)Math.Cos(rotY + Math.PI / 2);
                        break;
                    }

                case (Keys.A):
                    {
                        camPosition.X -= moveSpeed * (float)Math.Sin(rotY + Math.PI / 2);
                        camPosition.Z -= moveSpeed * (float)Math.Cos(rotY + Math.PI / 2);
                        break;
                    }
                case (Keys.Q):
                    {
                        rotY -= turnSpeed;
                        break;
                    }
                case (Keys.E):
                    {
                        rotY += turnSpeed;
                        break;
                    }
                case (Keys.Up):
                    {
                        if(rotXZ < Math.PI / 2)
                        {
                            rotXZ += turnSpeed;
                        }                            
                        break;
                    }
                case (Keys.Down):
                    {
                        if (rotXZ > -Math.PI / 2)
                        {
                            rotXZ -= turnSpeed;
                        }
                        break;
                    }


            }
        }
        private void OnMouseScroll(object sender, MouseEventArgs e)
        {
            camPosition.Y -= e.Delta * 0.001f;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMiddleMouseDown)
            {
                rotY = tempY + e.X * turnSpeed;

                float tmp = tempXZ - e.Y * turnSpeed / 4;
                if (tmp < Math.PI / 2 && tmp > -Math.PI / 2)
                {
                    rotXZ = tmp;
                }             

            }
            if (isLeftMouseDown)
            {
                Point mouseMoveLocation = new Point(e.X, e.Y);
                PickingTriangle(mouseMoveLocation);
            }
            
        }


        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case (MouseButtons.Middle):
                    {
                        tempY = rotY - e.X * turnSpeed;
                        tempXZ = rotXZ + e.Y * turnSpeed / 4;

                        isMiddleMouseDown = true;
                        break;
                    }
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = true;

                        Point mouseDownLocation = new Point(e.X, e.Y);
                        PickingTriangle(mouseDownLocation);
                        break;
                    }
            }
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case (MouseButtons.Middle):
                    {
                        isMiddleMouseDown = false;
                        break;
                    }
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = false;
                        break;
                    }
            }
        }


        private void PickingTriangle(Point mouseLocation)
        {
            IntersectInformation hitLocation;

            Vector3 near, far, direction;

            near = new Vector3(mouseLocation.X, mouseLocation.Y, 0);
            far = new Vector3(mouseLocation.X, mouseLocation.Y, 100);

            near.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            far.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);

            direction = near - far;

            for(int i=0; i<indCount; i += 3)
            {
                if(Geometry.IntersectTri(verts[indices[i]].Position, verts[indices[i+1]].Position, verts[indices[i+2]].Position,near, direction, out hitLocation))
                {
                    verts[indices[i]].Color = Color.Red.ToArgb();
                    verts[indices[i+1]].Color = Color.Red.ToArgb();
                    verts[indices[i+2]].Color = Color.Red.ToArgb();


                    verts[indices[i]].Position += new Vector3(0, raiseConst, 0);
                    verts[indices[i+1]].Position += new Vector3(0, raiseConst, 0);
                    verts[indices[i+2]].Position += new Vector3(0, raiseConst, 0);

                    vb.SetData(verts, 0, LockFlags.None);
                }
            }

        }


        private void solidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.Solid;
            solidToolStripMenuItem.Checked = true;
            wireframeToolStripMenuItem.Checked = false;
            pointToolStripMenuItem.Checked = false;
        }
        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.WireFrame;
            solidToolStripMenuItem.Checked = false;
            wireframeToolStripMenuItem.Checked = true;
            pointToolStripMenuItem.Checked = false;
        }
        private void pointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.Point;
            solidToolStripMenuItem.Checked = false;
            wireframeToolStripMenuItem.Checked = false;
            pointToolStripMenuItem.Checked = true;
        }

        private void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();

            invalidating = false;
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                backgroundColor = cd.Color;
            }
            invalidating = true;
            this.Invalidate();
        }

        private void loadHeightmapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadHeightmap();
            vb.SetData(verts, 0, LockFlags.None);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateVertex();
            vb.SetData(verts, 0, LockFlags.None);
        }
    }  
}