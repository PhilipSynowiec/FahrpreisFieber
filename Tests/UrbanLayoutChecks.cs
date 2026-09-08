using System;
using System.Collections.Generic;
using System.Diagnostics;
public static class UrbanLayoutChecks
{
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Main(){
 var watch=Stopwatch.StartNew();
 foreach(int seed in new[]{0,1,-1,12345,54321,int.MinValue,int.MaxValue}){
  CityLayout.ClearCache();var roads=new HashSet<(int x,int y)>();var lots=new Dictionary<(int,int),CityLots.Lot>();int courtyards=0,land=0,largeArea=0;
  for(int x=-64;x<64;x++)for(int y=-64;y<64;y++){
   if(CityLayout.IsRoad(seed,x,y)){roads.Add((x,y));continue;}
   land++;
   if(!CityLots.TryGetLot(seed,x,y,out var lot)){courtyards++;continue;}
   Check(x>=lot.x && x<lot.x+lot.width && y>=lot.y && y<lot.y+lot.depth,"Invalid footprint membership");lots[(lot.AnchorX,lot.AnchorY)]=lot;if(lot.cells!=(lot.cells&(~lot.cells+1)))largeArea++;
  }
  int dead=0,edgeEnds=0,bends=0;
  foreach(var p in roads){int degree=0;foreach(var n in Neighbors(p)){if(CityLayout.IsRoad(seed,n.x,n.y))degree++;if(roads.Contains(n))edgeEnds++;}if(degree==1)dead++;if(degree>=3)bends++;}
  var seen=new HashSet<(int,int)>{(0,0)};var queue=new Queue<(int x,int y)>();queue.Enqueue((0,0));while(queue.Count>0){var p=queue.Dequeue();foreach(var n in Neighbors(p))if(roads.Contains(n)&&seen.Add(n))queue.Enqueue(n);}
  Check(seen.Count==roads.Count,"Disconnected streets");int loops=edgeEnds/2-roads.Count+1;Check(loops>100,"Too few alternate routes");Console.WriteLine($"GRAPH {seed}: {roads.Count} roads {loops} loops {dead} dead ends");Check(dead<roads.Count*.05,"Maze-like dead-end density");Check(largeArea>(land-courtyards)*.6,"Buildings remain mostly single-tile");Check(courtyards>0,"No courtyard space");
  int nonRectangular=0;
  foreach(var lot in lots.Values){bool frontage=false;int area=0;Check(lot.width<=4&&lot.depth<=4,"Overlong building span");for(int x=lot.x;x<lot.x+lot.width;x++)for(int y=lot.y;y<lot.y+lot.depth;y++){
   if(!lot.Contains(x,y))continue;area++;
   Check(!CityLayout.IsRoad(seed,x,y),"Building overlaps road");Check(CityLots.TryGetLot(seed,x,y,out var cell)&&cell.x==lot.x&&cell.y==lot.y&&cell.width==lot.width&&cell.depth==lot.depth&&cell.cells==lot.cells,"Overlapping parcel assignment");foreach(var n in Neighbors((x,y)))if(CityLayout.IsRoad(seed,n.x,n.y))frontage=true;
  }Check(frontage,"Building without street frontage");if(area<lot.width*lot.depth)nonRectangular++;
   var connected=new HashSet<(int,int)>{(lot.AnchorX,lot.AnchorY)};var pending=new Queue<(int x,int y)>();pending.Enqueue((lot.AnchorX,lot.AnchorY));while(pending.Count>0){var c=pending.Dequeue();foreach(var n in Neighbors(c))if(lot.Contains(n.x,n.y)&&connected.Add(n))pending.Enqueue(n);}Check(connected.Count==area,"Disconnected building wings");
  }
  Check(nonRectangular>100,"Missing nonrectangular buildings");
  var points=new List<(int x,int y)>(roads);var random=new Random(seed);
  for(int i=0;i<10;i++){
   var a=points[random.Next(points.Count)];var b=points[random.Next(points.Count)];var route=CityLayout.FindRoadPath(seed,a.x,a.y,b.x,b.y);Check(route.Count>0&&route[0]==a&&route[route.Count-1]==b,"Bad route endpoints");
   for(int j=1;j<route.Count;j++)Check(CityLayout.IsRoad(seed,route[j].x,route[j].y)&&Math.Abs(route[j].x-route[j-1].x)+Math.Abs(route[j].y-route[j-1].y)==1,"Route leaves graph");
   var distance=new Dictionary<(int,int),int>{{a,0}};queue.Enqueue(a);while(queue.Count>0){var p=queue.Dequeue();if(p==b)break;foreach(var n in Neighbors(p))if(!distance.ContainsKey(n)&&CityLayout.IsRoad(seed,n.x,n.y)){distance[n]=distance[p]+1;queue.Enqueue(n);}}queue.Clear();Check(distance[b]==route.Count-1,"A* not optimal");
  }
  CityLayout.ClearCache();foreach(var expected in lots.Values){Check(CityLots.TryGetLot(seed,expected.AnchorX,expected.AnchorY,out var actual)&&actual.width==expected.width&&actual.depth==expected.depth&&actual.cells==expected.cells,"Parcel changed on regeneration");}
  Console.WriteLine($"PASS {seed}: {roads.Count} roads, {loops} loops, {dead} dead ends, {lots.Count} street-front buildings ({nonRectangular} nonrectangular), {largeArea} tiles in larger buildings, {courtyards} courtyard/park tiles");
 }
 Console.WriteLine($"PASS: connected urban graph, shortest routing, nonoverlapping street-facing parcels; {watch.ElapsedMilliseconds}ms");
 }
 static IEnumerable<(int x,int y)> Neighbors((int x,int y) p){yield return(p.x-1,p.y);yield return(p.x+1,p.y);yield return(p.x,p.y-1);yield return(p.x,p.y+1);}
}
