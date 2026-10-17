using System;
using System.Collections.Generic;

public struct CutPoint
{
    public double X, Y;
    public CutPoint(double x,double y){X=x;Y=y;}
}
public static class CutStrokeEvaluator
{
    public static double Evaluate(IReadOnlyList<CutPoint> points,int acceptedCuts)
    {
        if(points==null || points.Count<3)return 0;
        double x=.3+.2*(acceptedCuts%3);
        CutPoint first=points[0],last=points[points.Count-1];
        if(Math.Abs(first.X-x)>.09 || Math.Abs(first.Y-.8)>.10 || Math.Abs(last.X-x)>.09 || Math.Abs(last.Y-.2)>.10)return 0;
        double deviation=0,length=0;
        for(int i=0;i<points.Count;i++)
        {
            if(double.IsNaN(points[i].X)||double.IsNaN(points[i].Y)||double.IsInfinity(points[i].X)||double.IsInfinity(points[i].Y))return 0;
            if(points[i].X<0 || points[i].X>1 || points[i].Y<0 || points[i].Y>1)return 0;
            deviation+=Math.Abs(points[i].X-x);
            if(i>0){double dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;length+=Math.Sqrt(dx*dx+dy*dy);}
        }
        if(length<.45)return 0;
        // Alignment plus extra path length penalizes unstable scribbling, independent of resolution.
        return Math.Max(0,Math.Min(1,1-deviation/points.Count/.12-Math.Max(0,length-.7)*.5));
    }
}
