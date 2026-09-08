using System.Collections.Generic;
using UnityEngine;

// One continuous exterior for an arbitrary tile footprint. No walls between
// adjoining wings; windows are emitted only along exposed facade segments.
public class BuildingLotVisual : MonoBehaviour
{
    private Mesh mesh;
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector3> normals = new();
    private readonly List<int>[] triangles = {new(),new(),new(),new()};
    private CityLots.Lot lot;
    private float height;
    private const float Setback=1.7f;

    public void Build(GameObject source, CityLots.Lot footprint, float tileSize)
    {
        lot=footprint;
        var facade=source.transform.Find("Facade");height=facade.localScale.y;
        var roof=source.transform.Find("Roof cap");
        var glass=source.transform.Find("Front windows");
        var pavement=source.transform.Find("Pavement");
        var materials=new[]{facade.GetComponent<Renderer>().sharedMaterial,roof.GetComponent<Renderer>().sharedMaterial,
            glass.GetComponent<Renderer>().sharedMaterial,pavement.GetComponent<Renderer>().sharedMaterial};
        for(int y=lot.y;y<lot.y+lot.depth;y++)for(int x=lot.x;x<lot.x+lot.width;x++)
        {
            if(!lot.Contains(x,y))continue;
            Rect r=CellRect(x,y);float cx=(x-lot.x)*10,cz=(y-lot.y)*10;
            Horizontal(cx-5,cz-5,cx+5,cz+5,.16f,3);
            Horizontal(r.xMin,r.yMin,r.xMax,r.yMax,height+.16f,1);
            Edge(x,y,-1,0,r.xMin,r.yMin,r.yMax,Vector3.left);
            Edge(x,y,1,0,r.xMax,r.yMin,r.yMax,Vector3.right);
            Edge(x,y,0,-1,r.yMin,r.xMin,r.xMax,Vector3.back);
            Edge(x,y,0,1,r.yMax,r.xMin,r.xMax,Vector3.forward);
            var collider=gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(r.center.x,height*.5f+.16f,r.center.y);collider.size=new Vector3(r.width,height,r.height);
        }
        mesh=new Mesh {name="Building footprint"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.subMeshCount=4;
        for(int i=0;i<4;i++)mesh.SetTriangles(triangles[i],i);
        mesh.RecalculateBounds();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;
        transform.localScale=Vector3.one*(tileSize/10f);
        vertices.Clear();normals.Clear();foreach(var list in triangles)list.Clear();
    }
    private Rect CellRect(int x,int y)
    {
        float cx=(x-lot.x)*10,cz=(y-lot.y)*10;
        return Rect.MinMaxRect(cx-5+(lot.Contains(x-1,y)?0:Setback),cz-5+(lot.Contains(x,y-1)?0:Setback),
            cx+5-(lot.Contains(x+1,y)?0:Setback),cz+5-(lot.Contains(x,y+1)?0:Setback));
    }
    private void Edge(int x,int y,int dx,int dy,float plane,float start,float end,Vector3 normal)
    {
        if(!lot.Contains(x+dx,y+dy)){Wall(plane,start,end,dx!=0,normal);return;}
        Rect neighbor=CellRect(x+dx,y+dy);
        float from=dx!=0?neighbor.yMin:neighbor.xMin,to=dx!=0?neighbor.yMax:neighbor.xMax;
        // At an inset corner, only the uncovered portions of the shared edge are exterior.
        if(from>start)Wall(plane,start,Mathf.Min(end,from),dx!=0,normal);
        if(to<end)Wall(plane,Mathf.Max(start,to),end,dx!=0,normal);
    }
    private void Wall(float plane,float start,float end,bool vertical,Vector3 normal)
    {
        if(end-start<.001f)return;
        Vector3 a=vertical?new Vector3(plane,.16f,start):new Vector3(start,.16f,plane);
        Vector3 b=vertical?new Vector3(plane,.16f,end):new Vector3(end,.16f,plane);
        Quad(a,b,b+Vector3.up*height,a+Vector3.up*height,normal,0);
        Quad(a+Vector3.up*(height-.14f),b+Vector3.up*(height-.14f),b+Vector3.up*height,a+Vector3.up*height,normal,1,.01f);
        if(end-start<.8f)return;
        Vector3 inset=(b-a).normalized*.25f;a+=inset;b-=inset;
        for(float level=1.1f;level<height-.6f;level+=2f)
            Quad(a+Vector3.up*level,b+Vector3.up*level,b+Vector3.up*(level+.65f),a+Vector3.up*(level+.65f),normal,2,.02f);
    }
    private void Horizontal(float x0,float z0,float x1,float z1,float y,int material)=>
        Quad(new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0),Vector3.up,material);
    private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,int material,float inset=0)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=d;d=swap;}
        int i=vertices.Count;foreach(var v in new[]{a,b,c,d}){vertices.Add(v+normal*inset);normals.Add(normal);}
        triangles[material].AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
