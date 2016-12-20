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
using Chromodoris.MeshTools;
using System.Linq;
using Grasshopper.Kernel.Types;

namespace Chromodoris
{
    public class CurvatureApproximationComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IsoMesh class.
        /// </summary>
        public CurvatureApproximationComponent()
          : base("Curvature Approximation", "Curvature",
              "Very roughly approximates mesh curvature.",
              "Chromodoris", "Display")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The mesh to approximate.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Minimum Radius", "MinR", "The approximate maximum curvature radius.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Maximum Radius", "MaxR", "The approximate maximum curvature radius.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Mean Curvature", "Mean", "The mean curvature.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gaussian Curvature", "Gauss", "The gaussian curvature.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Minimum Curvature Direction", "MinD", "The direction of minimum curvature.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Maximum Curvature Direction", "MaxD", "The direction of maximum curvature.", GH_ParamAccess.list);
        }

        public double[] minRad;
        public double[] maxRad;
        public double[] mean;
        public double[] gaussian;
        public Vector3d[] minDirs;
        public Vector3d[] maxDirs;


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData("Mesh", ref mesh)) return;

            var c = new CurvatureApproximation(mesh);

            DA.SetDataList("Minimum Radius", c.minRad.Select(x => new GH_Number(x)));
            DA.SetDataList("Maximum Radius", c.maxRad.Select(x => new GH_Number(x)));
            DA.SetDataList("Mean Curvature", c.mean.Select(x => new GH_Number(x)));
            DA.SetDataList("Gaussian Curvature", c.gaussian.Select(x => new GH_Number(x)));
            DA.SetDataList("Minimum Curvature Direction", c.minDirs.Select(x => new GH_Vector(x)));
            DA.SetDataList("Maximum Curvature Direction", c.maxDirs.Select(x => new GH_Vector(x)));
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
                return Chromodoris.Properties.Resources.Icon_Curvature;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{2C926A8F-C979-4BD9-BF5F-5D9763FB3681}"); }
        }
    }
}
 