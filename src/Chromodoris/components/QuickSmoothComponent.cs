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

namespace Chromodoris.Components
{
    public class QuickSmoothComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public QuickSmoothComponent()
          : base("QuickSmooth", "Smooth",
              "A quick vertex smoothing algorithm. Averages neighbouring vertex locations.",
              "Chromodoris", "Smoothing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddMeshParameter("Mesh", "M", "The mesh to smooth.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step", "S", "The step size.", GH_ParamAccess.item, 0.5);
            pManager.AddIntegerParameter("Iterations", "I", "The number of smoothing iterations.", GH_ParamAccess.item, 1);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The smoothed mesh.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.Geometry.Mesh mesh = null;
            double step = 0.5;
            int iterations = 1;

            if (!DA.GetData(0, ref mesh)) return;
            DA.GetData(1, ref step);
            DA.GetData(2, ref iterations);

            if (iterations < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Iterations must be larger than 0.");
                return;
            }

            else if (step < 0 || step > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step must be between 0 and 1.");
                return;
            }

            MeshTools.VertexSmooth smooth = null;
            if (iterations > 0 & step > 0)
            {
                smooth = new MeshTools.VertexSmooth(mesh, step, iterations);
                mesh = smooth.compute();
            }
            DA.SetData(0, mesh);
            smooth = null;
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
                return Chromodoris.Properties.Resources.Icon_QuickSmooth;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3d8dfc65-6223-48c8-9e3f-93c510af6baa}"); }
        }
    }
}