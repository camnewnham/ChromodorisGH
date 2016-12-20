/*
 *      ___  _  _  ____   __   _  _   __  ____   __  ____  __  ____ 
 *     / __)/ )( \(  _ \ /  \ ( \/ ) /  \(    \ /  \(  _ \(  )/ ___)
 *    ( (__ ) __ ( )   /(  O )/ \/ \(  O )) D ((  O ))   / )( \___ \
 *     \___)\_)(_/(__\_) \__/ \_)(_/ \__/(____/ \__/(__\_)(__)(____/
 *
 *    Copyright Cameron Newnham 2015-2016
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU General Public License as published by
 *    the Free Software Foundation, either version 3 of the License, or
 *    (at your option) any later version.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *    GNU General Public License for more details.
 *
 *    You should have received a copy of the GNU General Public License
 *    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace Chromodoris.Components
{
    public class MeshPipeComponent : GH_Component
    {
        public MeshPipeComponent()
          : base("Mesh Pipe", "MPipe",
              "Pipes multiple curves into a single mesh.",
              "Chromodoris", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddCurveParameter("Polylines", "P", "The polylines to pipe.", GH_ParamAccess.list);
            pManager[pManager.AddIntegerParameter("Number of Sides", "N", "The number of sides for the pipe.", GH_ParamAccess.item, 4)].Optional = true;
            pManager.AddNumberParameter("Radius", "R", "The radius of the pipe", GH_ParamAccess.item);
            pManager[pManager.AddBooleanParameter("Cap", "C", "Cap the loft.", GH_ParamAccess.item, false)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The mesh loft.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Curve> crvs = new List<Curve>();
            List<Polyline> pls = new List<Polyline>();
            int numSides = 4;
            double radius = 1;
            bool cap = false;

            if (!DA.GetDataList("Polylines", crvs)) return;
            DA.GetData("Cap", ref cap);
            DA.GetData("Number of Sides", ref numSides);
            if (!DA.GetData("Radius", ref radius)) return;

            foreach (var c in crvs)
            {
                Polyline p;
                if (c.TryGetPolyline(out p)) {
                    pls.Add(p);
                }
                else
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must be polylines.");
                    return;
                }
            }
            
            DA.SetData("Mesh", new GH_Mesh(GetPiped(pls, numSides, radius, cap)));
        }

        public Mesh GetPiped(List<Polyline> polylines, int numSides, double radius, bool cap)
        {
            var pt = new Point3d(radius, 0, 0);
            Polyline polygon = new Polyline();

            Point3d curr = new Point3d(pt);
            polygon.Add(curr);
            var xfm = Transform.Rotation(Math.PI * 2 / (double)numSides, Point3d.Origin);
            for (int i = 0; i < numSides; i++)
            {
                var pNew = new Point3d(curr);
                pNew.Transform(xfm);

                polygon.Add(pNew);
                curr = pNew;
            }

            var multimesh = new Mesh[polylines.Count];

            System.Threading.Tasks.Parallel.For(0, polylines.Count, pc => {
                Polyline pl = polylines[pc];
                List<Plane> frames = new List<Plane>();
                for (int i = 0; i < pl.Count; i++)
                {
                    Point3d o = pl[i];
                    Vector3d dir = Vector3d.Unset;
                    if (i == 0)
                    {
                        if (!pl.IsClosed)
                        {
                            dir = pl[1] - pl[0];
                            dir.Unitize();
                        }
                        else
                        {
                            Vector3d prevDir = pl[0] - pl[pl.Count-2];
                            Vector3d nextDir = pl[1] - pl[0];
                            prevDir.Unitize();
                            nextDir.Unitize();
                            dir = (prevDir + nextDir) / 2;
                            dir.Unitize();
                        }
                    }
                    else if (i == pl.Count - 1)
                    {
                        if (!pl.IsClosed)
                        {
                            dir = pl[pl.Count - 1] - pl[pl.Count - 2];
                            dir.Unitize();
                        }
                    }
                    else
                    {
                        Vector3d prevDir = pl[i] - pl[i - 1];
                        Vector3d nextDir = pl[i + 1] - pl[i];
                        prevDir.Unitize();
                        nextDir.Unitize();
                        dir = (prevDir + nextDir) / 2;
                        dir.Unitize();
                    }

                    if (frames.Count > 0)
                    {
                        if (dir != Vector3d.Unset)
                        {
                            var prevFrame = frames.Last();
                            var newPlane = new Plane(o, dir);
                            double rotAng = Vector3d.VectorAngle(prevFrame.XAxis, newPlane.XAxis, newPlane);
                            newPlane.Rotate(-rotAng, newPlane.ZAxis);
                            frames.Add(newPlane);
                        }
                    }
                    else
                    {
                        frames.Add(new Plane(o, dir));
                    }
                }

                Mesh mesh = new Mesh();
                Mesh capm = null;
                var poly = new Polyline(polygon);
                poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, frames.First()));

                int[] lastVerts = new int[numSides];
                for (int p = 0; p < poly.Count - 1; p++)
                {
                    lastVerts[p] = mesh.Vertices.Add(poly[p]);
                }

                int[] firstVerts = lastVerts;
                
                if (cap && !pl.IsClosed)
                {
                    capm = Mesh.CreateFromClosedPolyline(poly);
                }

                for (int i = 1; i < frames.Count; i++)
                {
                    poly = new Polyline(polygon);
                    poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, frames[i]));

                    int[] newVerts = new int[numSides];
                    for (int p = 0; p < poly.Count - 1; p++)
                    {
                        newVerts[p] = mesh.Vertices.Add(poly[p]);
                    }

                    for (int v = 1; v <= numSides; v++)
                    {

                        if (v == numSides)
                        {
                            mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[0], newVerts[0], newVerts[v - 1]);
                        }
                        else
                        {
                            mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[v], newVerts[v], newVerts[v - 1]);

                        }
                    }

                    lastVerts = newVerts;
                }

                if (pl.IsClosed)
                {

                    // resolve any twists

                    /*
                    int len = firstVerts.Length;
                    int[] shifted = new int[len];
                    int shift = (int) Math.Floor(len / (double) 2);
                    for (int s = 0; s < len; s++)
                    {
                        int pos = (s + shift) % len;
                        shifted[pos] = firstVerts[s];
                    }
                    
                    firstVerts = shifted;
                    */

                    for (int v = 1; v <= numSides; v++)
                    {
                        if (v == numSides)
                        {
                            mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[0], firstVerts[0], firstVerts[v - 1]);
                        }
                        else
                        {
                            mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[v], firstVerts[v], firstVerts[v - 1]);

                        }
                    }
                } else if (cap)
                {
                    capm.Append(Mesh.CreateFromClosedPolyline(poly));
                    mesh.Append(capm);
                }

                multimesh[pc] = mesh;
            });

            Mesh combined = new Mesh();
            foreach (var m in multimesh)
            {
                combined.Append(m);
            }

            return combined;
        }
            
            
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Chromodoris.Properties.Resources.Icon_MeshPipe;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{4A27BFF9-C010-4A16-B8F5-1ACC73961AD6}"); }
        }
    }
}