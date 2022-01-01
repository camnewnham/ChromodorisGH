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

using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Chromodoris
{
    public class VoxelSampleComponent : GH_Component
    {
        public VoxelSampleComponent()
          : base("Sample Voxels", "VoxelSample",
              "Construct and sample a voxel grid from a point cloud and optional charges.",
              "Chromodoris", "Isosurface")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points to sample.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Charges", "C", "Charge values corresponding to each point.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Voxel Size", "S", "Size of each voxel.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Effective Range", "R", "The maximum search range for voxel sampling.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Density Sampling", "D", "Toggle point density affecting the point values", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Linear Sampling", "L", "Toggle falloff from exponential to linear", GH_ParamAccess.item, true);
            pManager[1].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
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
            double cellSize = 0;
            double range = 0;
            bool bulge = false;
            bool linear = true;

            if (!DA.GetDataList(0, points))
            {
                return;
            }

            DA.GetDataList(1, charges); // Optional
            if (!DA.GetData(2, ref cellSize))
            {
                return;
            }

            if (!DA.GetData(3, ref range))
            {
                return;
            }

            DA.GetData(4, ref bulge); // Optional
            DA.GetData(5, ref linear); // Optional

            if (range <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Range must be larger than 0.");
                return;
            }

            if (charges.Count != points.Count && charges.Count != 0 && charges.Count != 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The number of charges should be 0, 1, or equal to the number of points.");
            }

            VoxelSampler sampler = new VoxelSampler(points, charges, cellSize, range, bulge, linear);
            sampler.Initialize();

            // choose execution mode
            // if points < voxels/2, sample from points
            // otherwise sample frm voxels
            if (points.Count < sampler.xRes * sampler.yRes * sampler.yRes / 2)
            {
                sampler.ExecuteInverseMultiThread();
            }
            else
            {
                sampler.ExecuteMultiThread();
            }

            DA.SetData(0, sampler.Box);
            DA.SetData(1, sampler.Data);
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Chromodoris.Properties.Resources.Icon_VoxelSample;

        public override Guid ComponentGuid => new Guid("{40821789-6a28-41ad-bc33-55d281769713}");
    }
}
