using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Godot.NativeInterop;
using Godot.Collections;

public class MultiMeshinst
{
    public MultiMesh multimesh;
    public MultiMeshInstance2D multimeshInstance;

    public Mesh mesh { get; private set; }

    public int maxInstances { get; private set; }
    public int visibleInstances { get; private set; }
    float[] buffer;


    public MultiMeshinst(Mesh mesh, int maxInstances,int visibleInstances, Node2D parent)
    {
        this.mesh = mesh;
        this.maxInstances = maxInstances;
        this.visibleInstances = visibleInstances;

        initialiseMultimesh();
        parent.AddChild(multimeshInstance);
        
    }

    public void initialiseMultimesh()
    {
        multimesh = new MultiMesh();
        multimesh.UseColors = true;
        multimesh.UseCustomData = false;
        multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
        multimesh.InstanceCount = maxInstances;
        multimesh.VisibleInstanceCount = visibleInstances;
        multimesh.Mesh = mesh;

        multimeshInstance = new MultiMeshInstance2D();
        multimeshInstance.Multimesh = multimesh;
        multimesh.Buffer = new float[maxInstances * 12];
    }

    public void UpdateTransform(int populationCount, Vector2[] positions, Color[] colors)
    {
        multimesh.VisibleInstanceCount = populationCount;
        buffer=multimesh.Buffer;
		Parallel.For(0, populationCount, i =>
		{
			int offset = i * 12; // 12 floats per instance

			// Identity rotation/scale, only setting position
			buffer[offset + 0] = 1f;           // x.x
			buffer[offset + 1] = -0f;           // y.x
			buffer[offset + 2] = 0f;           // padding
			buffer[offset + 3] = positions[i].X; // origin.x

			buffer[offset + 4] = 0f;           // x.y
			buffer[offset + 5] = 1f;           // y.y
			buffer[offset + 6] = 0f;           // padding
            buffer[offset + 7] = positions[i].Y; // origin.y
            
            //colour update 
            buffer[offset + 8] = colors[i].R;
            buffer[offset + 9] = colors[i].G;  
            buffer[offset + 10] = colors[i].B;
            buffer[offset + 11] = colors[i].A;
		});
		multimesh.Buffer = buffer;
    }


}
