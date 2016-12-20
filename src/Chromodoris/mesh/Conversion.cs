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

using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromodoris.MeshTools
{
    public class Conversion
    {
        public static Mesh PlanesToMesh(List<Plane> planes, double radius)
        {
            Mesh m = new Mesh();

            foreach (Plane p in planes)
            {
                Vector3d x2 = p.XAxis;
                x2.Unitize();
                x2 *= radius;
                Vector3d y2 = p.YAxis;
                y2.Unitize();
                y2 *= radius;
                Point3d o = p.Origin;

                int v0 = m.Vertices.Add(o - x2 - y2);
                int v1 = m.Vertices.Add(o - x2 + y2);
                int v2 = m.Vertices.Add(o + x2 + y2);
                int v3 = m.Vertices.Add(o + x2 - y2);

                m.Faces.AddFace(v0, v1, v2, v3);
                m.FaceNormals.AddFaceNormal(p.Normal);
            }

            return m;
        }
        
        public static bool MeshToPlanes(Mesh mesh, ref List<Plane> planes)
        {
            foreach (MeshFace f in mesh.Faces)
            {
                if (f.IsQuad)
                {
                    Point3d v0 = mesh.Vertices[f.A];
                    Point3d v1 = mesh.Vertices[f.B];
                    Point3d v2 = mesh.Vertices[f.C];
                    Point3d v3 = mesh.Vertices[f.D];

                    Point3d ctr = (v0 + v1 + v2 + v3) / 4;
                    Vector3d x = v2 - v1;
                    Vector3d y = v1 - v0;

                    planes.Add(new Plane(ctr, x, y));
                } else
                {
                    return false;
                }
            }
            return true;
        }

        public static List<Plane> MeshesToPlanes(List<Mesh> meshes)
        {
            List<Plane> planes = new List<Plane>();
            foreach (var m in meshes)
            {
                MeshToPlanes(m, ref planes);
            }
            return planes;
        }

    }
}
