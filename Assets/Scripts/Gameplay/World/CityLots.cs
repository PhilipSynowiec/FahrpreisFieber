using System.Collections.Generic;

// Compact, connected street-front footprints, independent of streamed meshes.
public static class CityLots
{
    public readonly struct Lot
    {
        public readonly int x, y, width, depth;
        public readonly ulong cells;
        public bool Exists => cells != 0;
        public int AnchorX { get { for (int i=0;i<width*depth;i++) if ((cells & (1UL<<i))!=0) return x+i%width; return x; } }
        public int AnchorY { get { for (int i=0;i<width*depth;i++) if ((cells & (1UL<<i))!=0) return y+i/width; return y; } }
        public bool Contains(int gx, int gy) => gx>=x && gx<x+width && gy>=y && gy<y+depth && (cells & (1UL<<((gy-y)*width+gx-x)))!=0;
        public Lot(int x, int y, int width, int depth, ulong cells) { this.x=x;this.y=y;this.width=width;this.depth=depth;this.cells=cells; }
    }
    private static readonly Dictionary<(int seed, int x, int y), Lot[]> cache = new();
    private static readonly Queue<(int seed, int x, int y)> order = new();
    public static void ClearCache() { cache.Clear(); order.Clear(); }
    public static bool TryGetLot(int seed, int x, int y, out Lot lot)
    {
        var key=(seed,CityLayout.Region(x),CityLayout.Region(y));
        if(!cache.TryGetValue(key,out var lots))
        {
            lots=Generate(seed,key.Item2,key.Item3);
            if(cache.Count>=256)cache.Remove(order.Dequeue());
            cache.Add(key,lots);order.Enqueue(key);
        }
        lot=lots[CityLayout.PositiveMod(y,CityLayout.RegionSize)*CityLayout.RegionSize+CityLayout.PositiveMod(x,CityLayout.RegionSize)];
        return lot.Exists;
    }
    private static Lot[] Generate(int seed,int rx,int ry)
    {
        int size=CityLayout.RegionSize,ox=rx*size,oy=ry*size;
        var lots=new Lot[size*size];var occupied=new bool[size*size];var frontages=new List<int>();
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            int gx=ox+x,gy=oy+y,i=y*size+x;
            int px=(gx-CityLayout.PositiveMod(gx,5))/5,py=(gy-CityLayout.PositiveMod(gy,5))/5;
            occupied[i]=CityLayout.IsRoad(seed,gx,gy)||CityLayout.Hash(seed^0x6213,px,py)%7==0;
            if(!occupied[i]&&(CityLayout.IsRoad(seed,gx-1,gy)||CityLayout.IsRoad(seed,gx+1,gy)||CityLayout.IsRoad(seed,gx,gy-1)||CityLayout.IsRoad(seed,gx,gy+1)))frontages.Add(i);
        }
        frontages.Sort((a,b)=>{int c=CityLayout.Hash(seed^0x4623,ox+a%size,oy+a/size).CompareTo(CityLayout.Hash(seed^0x4623,ox+b%size,oy+b/size));return c!=0?c:a.CompareTo(b);});
        foreach(int start in frontages)
        {
            if(occupied[start])continue;
            var footprint=new HashSet<int>{start};occupied[start]=true;
            int minX=start%size,maxX=minX,minY=start/size,maxY=minY;
            int target=4+(int)(CityLayout.Hash(seed^0x2845,ox+minX,oy+minY)%7);
            while(footprint.Count<target)
            {
                int best=-1;double bestScore=double.MinValue;
                foreach(int cell in footprint)for(int direction=0;direction<4;direction++)
                {
                    int x=cell%size+(direction==0?-1:direction==1?1:0),y=cell/size+(direction==2?-1:direction==3?1:0);
                    if(x<0||y<0||x>=size||y>=size||occupied[y*size+x])continue;
                    int w=System.Math.Max(maxX,x)-System.Math.Min(minX,x)+1,d=System.Math.Max(maxY,y)-System.Math.Min(minY,y)+1;
                    // Bound both span and slenderness so long strips become separate buildings.
                    if(w>4||d>4||System.Math.Max(w,d)>System.Math.Min(w,d)*2+1)continue;
                    int index=y*size+x,neighbors=0;
                    if(x>0&&footprint.Contains(index-1))neighbors++;
                    if(x+1<size&&footprint.Contains(index+1))neighbors++;
                    if(y>0&&footprint.Contains(index-size))neighbors++;
                    if(y+1<size&&footprint.Contains(index+size))neighbors++;
                    double score=CityLayout.Hash(seed^start,ox+x,oy+y)/(double)uint.MaxValue + neighbors*.18 - (w*d-footprint.Count-1)*.025;
                    if(score>bestScore || (score==bestScore && index<best)){best=index;bestScore=score;}
                }
                if(best<0)break;
                footprint.Add(best);occupied[best]=true;
                minX=System.Math.Min(minX,best%size);maxX=System.Math.Max(maxX,best%size);minY=System.Math.Min(minY,best/size);maxY=System.Math.Max(maxY,best/size);
            }
            int width=maxX-minX+1,depth=maxY-minY+1;ulong mask=0;
            foreach(int cell in footprint)mask|=1UL<<((cell/size-minY)*width+cell%size-minX);
            var lot=new Lot(ox+minX,oy+minY,width,depth,mask);
            foreach(int cell in footprint)lots[cell]=lot;
        }
        return lots;
    }
}
