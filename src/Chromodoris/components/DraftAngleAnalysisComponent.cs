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
using System.Drawing;

namespace Chromodoris
{
    public class DraftAngleDisplayComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IsoMesh class.
        /// </summary>
        public DraftAngleDisplayComponent()
          : base("Draft Angle", "Draft",
              "Displays the draft angle of a mesh.",
              "Chromodoris", "Display")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The mesh to approximate.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Draft", "M", "The minimum draft angle.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum Draft", "M", "The maximum draft angle.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The coloured mesh.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            double minDraft = -45;
            double maxDraft = 45;
            if (!DA.GetData("Minimum Draft", ref minDraft)) return;
            if (!DA.GetData("Maximum Draft", ref maxDraft)) return;
            if (!DA.GetData("Mesh", ref mesh)) return;

            mesh.Normals.ComputeNormals();
            mesh.VertexColors.Clear();
            for (int i=0; i<mesh.Normals.Count; i++)
            {
                Vector3d norm = mesh.Normals[i];
                Vector3d refang = new Vector3d(norm.X, norm.Y, 0);
                refang.Unitize();
                double ang = Vector3d.VectorAngle(norm, refang);
                if (norm.Z < 0) ang = -ang;
                double degAng = Rhino.RhinoMath.ToDegrees(ang);
                if (degAng < minDraft) degAng = minDraft;
                else if (degAng > maxDraft) degAng = maxDraft;
                mesh.VertexColors.Add(MapRainbowColor(degAng, minDraft, maxDraft));
            }
            DA.SetData("Mesh", new GH_Mesh(mesh));
        }

        private Color MapRainbowColor( double value, double red_value, double blue_value)
        {
            // Convert into a value between 0 and 1023.
            int int_value = (int)(1023 * (value - red_value) /
                (blue_value - red_value));

            // Map different color bands.
            if (int_value < 256)
            {
                // Red to yellow. (255, 0, 0) to (255, 255, 0).
                return Color.FromArgb(255, int_value, 0);
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return Color.FromArgb(255 - int_value, 255, 0);
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return Color.FromArgb(0, 255, int_value);
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return Color.FromArgb(0, 255 - int_value, 255);
            }
        }


        public double Remap(double OldValue, double OldMin, double OldMax, double NewMin, double NewMax)
        {
            return (((OldValue - OldMin) * (NewMax - NewMin)) / (OldMax - OldMin)) + NewMin;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Chromodoris.Properties.Resources.Icons_DraftAngle;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{690B34AA-4E1D-45C1-83FE-F38B0FA7ED5C}"); }
        }
    }
}
 