using System;
using System.Collections.Generic;

public struct CutPoint
{
    public double X, Y;
    public CutPoint(double x,double y){X=x;Y=y;}
}
// Grades one downward knife stroke against the active guide line. Board-normalized coordinates (0-1).
// Tuned after pilot testing: students on phones start a little off the marker and stop short of FINISH,
// so the stroke only has to begin near the top of the guide and travel most of the way down it.
public static class CutStrokeEvaluator
{
    public const double StartTolerance=.15, EndTolerance=.18, MinDrop=.40, Spread=.25;
    public static double Evaluate(IReadOnlyList<CutPoint> points,int acceptedCuts)
    {
        if(points==null || points.Count<3)return 0;
        double x=.3+.2*(acceptedCuts%3);
        for(int i=0;i<points.Count;i++)
            if(double.IsNaN(points[i].X)||double.IsNaN(points[i].Y)||double.IsInfinity(points[i].X)||double.IsInfinity(points[i].Y))return 0;
        CutPoint first=points[0],last=points[points.Count-1];
        if(Math.Abs(first.X-x)>StartTolerance || first.Y<.55)return 0;
        double drop=first.Y-last.Y;
        if(drop<MinDrop || Math.Abs(last.X-x)>EndTolerance)return 0;
        double deviation=0,length=0;
        for(int i=0;i<points.Count;i++)
        {
            deviation+=Math.Abs(Math.Max(0,Math.Min(1,points[i].X))-x);
            if(i>0){double dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;length+=Math.Sqrt(dx*dx+dy*dy);}
        }
        // Alignment plus extra path length penalizes scribbling, independent of resolution.
        return Math.Max(0,Math.Min(1,1-deviation/points.Count/Spread-Math.Max(0,length-drop*1.6)*.4));
    }
}
