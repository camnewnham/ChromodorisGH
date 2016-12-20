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

namespace Chromodoris
{
    public class IsosurfaceComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IsoMesh class.
        /// </summary>
        public IsosurfaceComponent()
          : base("Build IsoSurface", "IsoSurface",
              "Constructs a 3D isosurface from voxel data (float[x,y,z]) and a box.",
              "Chromodoris", "Isosurface")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBoxParameter("Box", "B", "The bounding box.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Voxel Data", "D", "Voxelization data formatted as double[x,y,z].", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sample Value", "V", "The value to sample at, ie. IsoValue", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("Merge Vertices", "M", "Combine (weld) the mesh.", GH_ParamAccess.item, true);
            //pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("IsoSurface", "M", "The generated isosurface.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Box box = new Box();
            //bool merge = true;
            double isovalue = 0;
            float[,,] voxelData = null;

            if (!DA.GetData(0, ref box))   return;
            if (!DA.GetData(1, ref voxelData)) return;
            if (!DA.GetData(2, ref isovalue)) return;
            //DA.GetData(3, ref merge);

            
            VolumetricSpace vs = new VolumetricSpace(voxelData);
            HashIsoSurface isosurface = new HashIsoSurface(vs);
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            
            isosurface.computeSurfaceMesh(isovalue, ref mesh);
            transformMesh(mesh, box, voxelData);
            DA.SetData(0, mesh);

            voxelData = null;
            vs = null;
            isosurface = null;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Chromodoris.Properties.Resources.Icon_Isosurface;
               // return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{8726c6b0-f222-4fd9-9882-dd0cd0067988}"); }
        }

        public void transformMesh(Rhino.Geometry.Mesh mesh, Box _box, float[,,] data)
        {


            int x = data.GetLength(0)-1;
            int y = data.GetLength(1)-1;
            int z = data.GetLength(2)-1;


            Box gridBox = new Box(Plane.WorldXY, new Interval(0, x), new Interval(0, y), new Interval(0, z));
            gridBox.RepositionBasePlane(gridBox.Center);

            var trans = Transform.PlaneToPlane(gridBox.Plane, _box.Plane);
            trans = trans * Transform.Scale(gridBox.Plane, _box.X.Length / gridBox.X.Length, _box.Y.Length / gridBox.Y.Length, _box.Z.Length / gridBox.Z.Length);

            mesh.Transform(trans);
            mesh.Faces.CullDegenerateFaces();
        }
    }
}
 