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

namespace Chromodoris
{
    public class VoxelSampleCustomComponent : GH_Component
    {
        public VoxelSampleCustomComponent()
          : base("Sample Voxels (Custom)", "VoxelSample(C)",
              "Construct and sample a voxel grid from a point cloud and optional charges, using a specified box and dimensional data.",
              "Chromodoris", "Isosurface")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points to sample.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Charges", "C", "Charge values corresponding to each point.", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "The box representing the voxel grid.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("X Resolution", "X", "The number of grid cells in the X-direction.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Y Resolution", "Y", "The number of grid cells in the Y-direction.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Z Resolution", "Z", "The number of grid cells in the Z-direction.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Effective Range", "R", "The maximum search range for voxel sampling.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Density Sampling", "D", "Toggle point density affecting the point values", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Linear Sampling", "L", "Toggle falloff from exponential to linear", GH_ParamAccess.item, true);
            pManager[1].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Box", "B", "The generated box representing voxel grid.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Voxel Data", "D", "Voxel data as float[x,y,z]", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> points = new List<Point3d>();
            List<double> charges = new List<double>();
            int xr = 0;
            int yr = 0;
            int zr = 0;
            Box box = new Box();
            double range = 0;
            bool bulge = false;
            bool linear = true;

            if (!DA.GetDataList("Points", points)) return;
            DA.GetDataList("Charges", charges); // Optional
            if (!DA.GetData("Box", ref box)) return;
            if (!DA.GetData("X Resolution", ref xr)) return;
            if (!DA.GetData("Y Resolution", ref yr)) return;
            if (!DA.GetData("Z Resolution", ref zr)) return;
            if (!DA.GetData("Effective Range", ref range)) return;
            DA.GetData("Density Sampling", ref bulge); // Optional
            DA.GetData("Linear Sampling", ref linear); // Optional

            if (range <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Range must be larger than 0.");
                return;
            }

            if (charges.Count != points.Count && charges.Count != 0 && charges.Count != 1) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The number of charges should be 0, 1, or equal to the number of points.");
            }

            VoxelSampler sampler = new VoxelSampler(points, charges, box, xr, yr, zr, range, bulge, linear);
            sampler.init();
            sampler.executeMultiThreaded();

            DA.SetData("Box", sampler.getBox());
            DA.SetData("Voxel Data", sampler.getData());

            sampler = null;
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Chromodoris.Properties.Resources.Icons_Isosurface_Custom;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{CAD3A975-A7C8-44AB-ACF2-D199B9A37C6A}"); }
        }
    }
}
