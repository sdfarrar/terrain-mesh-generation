using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceCreator : MonoBehaviour {

	[Range(1, 200)]
	public int resolution = 10;

	public Vector3 offset;
	public Vector3 rotation;

	[Range(0f, 1f)]
	public float strength = 1f;

	public bool damping;

	public float frequency = 1f;
	
	[Range(1, 8)]
	public int octaves = 1;

	[Range(1f, 4f)]
	public float lacunarity = 2f;

	[Range(0f, 1f)]
	public float persistence = 0.5f;

	[Range(1, 3)]
	public int dimensions = 3;

	public NoiseMethodType type;

	public Gradient coloring;
	public bool coloringForStrength;

	public bool showNormals;

	private Mesh mesh;
	private int currentResolution;

	private Vector3[] vertices;
	private Vector3[] normals;
	private Color[] colors;

	
	void OnEnable () {
		//if(mesh==null){
		//	mesh = new Mesh();
		//	mesh.name = "Surface Mesh";
		//	GetComponent<MeshFilter>().mesh = mesh;
		//}
		Init();
		Refresh();
	}

	public void Init(){
		if(mesh==null){
			mesh = new Mesh();
			mesh.name = "Surface Mesh";
			GetComponent<MeshFilter>().mesh = mesh;
		}
		Refresh();
	}
	
	public void Refresh () {
		if(resolution!=currentResolution){
			CreateGrid();
		}
		Quaternion q = Quaternion.Euler(rotation);
		Vector3 point00 = q * transform.TransformPoint(new Vector3(-0.5f,-0.5f)) + offset;
		Vector3 point01 = q * transform.TransformPoint(new Vector3( 0.5f,-0.5f)) + offset;
		Vector3 point10 = q * transform.TransformPoint(new Vector3(-0.5f, 0.5f)) + offset;
		Vector3 point11 = q * transform.TransformPoint(new Vector3( 0.5f, 0.5f)) + offset;

		NoiseMethod method = Noise.methods[(int)type][dimensions - 1];
		float stepSize = 1f / resolution;
		float amplitude = damping ? strength / frequency : strength;
		for(int v=0, y=0; y<=resolution; ++y){
			Vector3 point0 = Vector3.Lerp(point00, point01, y*stepSize);
			Vector3 point1 = Vector3.Lerp(point10, point11, y*stepSize);
			for(int x=0; x<=resolution; ++x, ++v){
				Vector3 point = Vector3.Lerp(point0, point1, x*stepSize);
				float sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence);
				sample = type == NoiseMethodType.Value ? (sample - 0.5f) : (sample * 0.5f);
				if(coloringForStrength){
					colors[v] = coloring.Evaluate(sample + 0.5f);
					sample *= amplitude;
				}else{
					sample *= amplitude;
					colors[v] = coloring.Evaluate(sample + 0.5f);
				}
				vertices[v].y = sample;
				colors[v] = coloring.Evaluate(sample + 0.5f);
			}
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		//mesh.RecalculateNormals();
		CalculateNormals();
		mesh.normals = normals;
		//normals = mesh.normals; // grab the recalculated normals
	}

	private void CalculateNormals(){
		for(int v=0, z=0; z<=resolution; ++z){
			for(int x=0; x<=resolution; ++x, ++v){
				//normals[v] = Vector3.Cross(
				//	new Vector3(0f, GetZDerivative(x, z), 1f),
				//	new Vector3(1f, GetZDerivative(x, z), 0f));
				normals[v] = new Vector3(-GetXDerivative(x, z), 1f, -GetZDerivative(x, z)).normalized;

			}
		}
	}

	private float GetXDerivative(int x, int z){
		int rowOffset = z * (resolution + 1);
		float left, right, scale;
		if(x>0){
			left = vertices[rowOffset + x - 1].y;
			if(x<resolution){ // vertices on the "inside" of the mesh
				right = vertices[rowOffset + x + 1].y;
				scale = 0.5f * resolution;
			}else{ // vertices on the end of the x axis for the mesh
				right = vertices[rowOffset + x].y;
				scale = resolution;
			}
		}else{ // vertices at the start of the x axis for the mesh
			left = vertices[rowOffset+x].y;
			right = vertices[rowOffset+x+1].y;
			scale = resolution;
		}
		return (right - left) * scale;
	}

	private float GetZDerivative(int x, int z){
		int rowLength = resolution + 1;
		float back, forward, scale;
		if(z > 0){
			back = vertices[(z-1) * rowLength + x].y;
			if(z < resolution){
				forward = vertices[(z+1) * rowLength + x].y;
				scale = 0.5f * resolution;
			}else{
				forward = vertices[z * rowLength + x].y;
				scale = resolution;
			}
		}else{
			back = vertices[z * rowLength + x].y;
			forward = vertices[(z+1) * rowLength + x].y;
			scale = resolution;
		}
		return (forward - back) * scale;
	}

	private void CreateGrid(){
		currentResolution = resolution;
		mesh.Clear();

		// (n+1)^2 vertices
		vertices = new Vector3[(resolution+1) * (resolution+1)];
		colors = new Color[vertices.Length];
		normals = new Vector3[vertices.Length];
		Vector2[] uv = new Vector2[vertices.Length];
		float stepSize = 1f / resolution;

		for(int v=0, z=0; z<=resolution; ++z){
			for(int x=0; x<=resolution; ++x, ++v){
				vertices[v] = new Vector3(x * stepSize-0.5f, 0f, z*stepSize-0.5f);
				colors[v] = Color.black;
				normals[v] = Vector3.up;
				uv[v] = new Vector2(x*stepSize, z*stepSize);
			}
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.normals = normals;
		mesh.uv = uv;

		// 6n^2 triangles
		int[] triangles = new int[6 * resolution * resolution];
		for(int t=0, v=0, y=0; y<resolution; ++y, ++v){
			for(int x=0; x<resolution; ++x, ++v, t+=6){
				triangles[t] = v;
				triangles[t + 1] = v + resolution + 1;
				triangles[t + 2] = v + 1;
				triangles[t + 3] = v + 1;
				triangles[t + 4] = v + resolution + 1;
				triangles[t + 5] = v + resolution + 2;
			}
		}
		mesh.triangles = triangles;
	}

	private void OnDrawGizmosSelected(){
		float scale = 1f / resolution;
		if(showNormals && vertices != null){
			Gizmos.color = Color.yellow;
			for(int v=0; v<vertices.Length; ++v){
				Gizmos.DrawRay(vertices[v], normals[v] * scale);
			}
		}
	}

}
