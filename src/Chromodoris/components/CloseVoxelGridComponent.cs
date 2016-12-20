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
    public class CloseVoxelGridComponent : GH_Component
    {
        public CloseVoxelGridComponent()
          : base("Close Voxel Data", "Close",
              "Closes a voxel grid.",
              "Chromodoris", "Isosurface")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "The voxel data to close as float[,,].", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "The closed data.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            float[,,] myDat = null;

            if (!DA.GetData("Data", ref myDat)) { 
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not parse input data as float[,,].");
                return;
            }

            int lx = myDat.GetLength(0);
            int ly = myDat.GetLength(1);
            int lz = myDat.GetLength(2);

            float[,,] newDat = new float[lx, ly, lz];

            for (int x = 0; x < lx; x++)
            {
                for (int y = 0; y < ly; y++)
                {
                    for (int z = 0; z < lz; z++)
                    {
                        if (x == 0 || y == 0 || z == 0 || x == lx - 1 || y == ly - 1 || z == lz - 1)
                        {
                            newDat[x, y, z] = 0;
                        }
                        else
                        {
                            newDat[x, y, z] = myDat[x, y, z];
                        }
                    }
                }
            }

            DA.SetData("Data", new GH_ObjectWrapper(newDat));
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Chromodoris.Properties.Resources.Icon_Close_Voxel_Grid;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{0669240F-C8A7-4EF0-9DB9-1B228DF0EAE3}"); }
        }
    }
}
