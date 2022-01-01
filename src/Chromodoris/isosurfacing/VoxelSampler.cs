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

namespace Chromodoris
{
    internal class VoxelSampler
    {
        public int xRes;
        public int yRes;
        public int zRes;

        private Box box;
        private double xSpace;
        private double ySpace;
        private double zSpace;

        private RTree rTree;
        private List<Point3d> points;
        private List<double> values;
        private double range;
        private double rangeSq;
        private bool bulge = false;
        private bool linear;

        private Transform xfm;

        private Transform xfmToGrid;
        private Transform xfmFromGrid;
        private bool useXfm = false;


        public VoxelSampler(List<Point3d> points, List<double> values, double cellSize, double range, bool bulge, bool linear)
        {
            this.points = points;
            this.values = values;
            this.range = range;
            rangeSq = this.range * this.range;
            this.bulge = bulge;
            this.linear = linear;
            CreateEnvironment(cellSize, out box, out xRes, out yRes, out zRes);
        }

        public VoxelSampler(List<Point3d> points, List<double> values, double cellSize, Box box, double range, bool bulge, bool linear)
        {
            this.points = points;
            this.values = values;
            this.range = range;
            rangeSq = this.range * this.range;
            this.bulge = bulge;
            this.linear = linear;
            CreateEnvironment(cellSize, box, out this.box, out xRes, out yRes, out zRes);

            if (this.box.Plane.ZAxis != Vector3d.ZAxis || this.box.Plane.YAxis != Vector3d.YAxis || this.box.Plane.XAxis != Vector3d.XAxis)
            {
                xfm = GetBoxTransform(this.box, xRes, yRes, zRes);

                useXfm = true;
            }

        }

        public VoxelSampler(List<Point3d> points, List<double> values, Box box, int resX, int resY, int resZ, double range, bool bulge, bool linear)
        {
            this.points = points;
            this.values = values;
            this.range = range;
            rangeSq = this.range * this.range;
            this.bulge = bulge;
            this.linear = linear;
            this.box = box;
            this.box.RepositionBasePlane(box.Center);
            xRes = resX;
            yRes = resY;
            zRes = resZ;
            if (this.box.Plane.ZAxis != Vector3d.ZAxis || this.box.Plane.YAxis != Vector3d.YAxis || this.box.Plane.XAxis != Vector3d.XAxis)
            {
                xfm = GetBoxTransform(this.box, xRes, yRes, zRes);
                useXfm = true;
            }
        }

        public Transform GetBoxTransform(Box box, int x, int y, int z)
        {
            Box gridBox = new Box(Plane.WorldXY, new Interval(0, x), new Interval(0, y), new Interval(0, z));
            gridBox.RepositionBasePlane(gridBox.Center);

            Transform trans = Transform.PlaneToPlane(gridBox.Plane, box.Plane);
            trans *= Transform.Scale(gridBox.Plane, box.X.Length / gridBox.X.Length, box.Y.Length / gridBox.Y.Length, box.Z.Length / gridBox.Z.Length);

            return trans;

        }

        public void Initialize()
        {
            xSpace = (box.X.Max - box.X.Min) / (xRes - 1);
            ySpace = (box.Y.Max - box.Y.Min) / (yRes - 1);
            zSpace = (box.Z.Max - box.Z.Min) / (zRes - 1);

            // fill empty variables

            if (values.Count == 1)
            {
                double val = values[0];
                for (int i = 0; i < points.Count - 1; i++)
                {
                    values.Add(val);
                }

            }
            else if (values.Count < points.Count)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    values.Add(1);
                }
            }

            // make transform from full box to scaled box
            // _box is the big box
            Box _gridbox = new Box(Plane.WorldXY, new Interval(0, xRes - 1), new Interval(0, yRes - 1), new Interval(0, zRes - 1));
            _gridbox.RepositionBasePlane(_gridbox.Center);
            xfmToGrid = BoxToBoxTransform(box, _gridbox);
            xfmFromGrid = BoxToBoxTransform(_gridbox, box);

            // forward sample tree init
            rTree = new RTree();
            int ind = 0;
            foreach (Point3d p in points)
            {
                rTree.Insert(p, ind);
                ind++;
            }


            Gdata = new float[xRes, yRes, zRes];
        }

        public Transform BoxToBoxTransform(Box source, Box target)
        {
            Transform trans = Transform.PlaneToPlane(source.Plane, target.Plane);
            trans *= Transform.Scale(source.Plane, target.X.Length / source.X.Length, target.Y.Length / source.Y.Length, target.Z.Length / source.Z.Length);
            return trans;
        }

        public Box Box => box;

        public float[,,] Data => Gdata;

        private void ExecuteSingleThread()
        {
            for (int z = 0; z < zRes; z++)
            {
                assignSection(z);
            }
        }

        public void ExecuteMultiThread()
        {
            System.Threading.Tasks.ParallelOptions pLel = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.ForEach(Enumerable.Range(0, zRes), pLel, z => assignSection(z));
        }

        public void ExecuteInverse()
        {
            for (int i = 0; i < points.Count; i++)
            {
                AssignPointValue(i);
            }
        }

        public void ExecuteInverseMultiThread()
        {
            System.Threading.Tasks.ParallelOptions pLel = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.For(0, points.Count, pLel, i => AssignPointValue(i));
        }

        public void AssignPointValue(int i)
        {
            Point3d p = points[i];
            // transform to box space
            Point3d ptX = new Point3d(p);
            ptX.Transform(xfmToGrid);

            int[] closeCell = new int[] { (int)ptX.X, (int)ptX.Y, (int)ptX.Z }; // round to find the closest cell
                                                                                // if the point falls outside the box, skip it
            if (ptX.X < 0 || ptX.X >= xRes || ptX.Y < 0 || ptX.Y >= yRes || ptX.Z < 0 || ptX.Z >= zRes)
            {
                return;
            }

            if (AssignValueFromScaledPoint(ptX, closeCell, values[i])) // first (center) value was applied
            {
                // got to here, point is within threshold, and initial value has been applied
                int indstep = 0;


                bool inprogress = true;
                while (inprogress)
                {
                    inprogress = false;
                    indstep++;
                    List<int[]> neighbours = GetNeighbouringCells(closeCell[0], closeCell[1], closeCell[2], indstep);
                    foreach (int[] cell in neighbours)
                    {
                        if (AssignValueFromScaledPoint(ptX, cell, values[i]))
                        {
                            inprogress = true;
                        }
                    }
                }
            }
        }

        public bool AssignValueFromScaledPoint(Point3d ptX, int[] closeCell, double scalar)
        {
            // get distance to the cell
            Vector3d closestVec = new Point3d(closeCell[0], closeCell[1], closeCell[2]) - ptX;

            // transform vector to world space
            closestVec.Transform(xfmFromGrid);

            if (linear)
            {
                double len = closestVec.Length;
                if (len > range)
                {
                    return false;
                }
                // assign a value to this cell
                AssignValuesToCell(closeCell[0], closeCell[1], closeCell[2], scalar / (len));
                return true;
            }
            else
            {
                double len = closestVec.SquareLength;
                if (len > rangeSq)
                {
                    return false;
                }

                AssignValuesToCell(closeCell[0], closeCell[1], closeCell[2], scalar / (len * len));
                return true;
            }


        }

        public List<int[]> GetNeighbouringCells(int cx, int cy, int cz, int indstep)
        {
            List<int[]> neighbours = new List<int[]>();

            for (int x = (cx) - indstep; x <= (cx) + indstep; x++)
            {
                for (int y = (cy) - indstep; y <= (cy) + indstep; y++)
                {
                    for (int z = (cz) - indstep; z <= (cz) + indstep; z++)
                    {
                        if (!(x < 0 || x >= xRes || y < 0 || y >= yRes || z < 0 || z >= zRes))
                        {
                            if (
                                    x == (cx) - indstep
                                || x == (cx) + indstep
                                || y == (cy) - indstep
                                || y == (cy) + indstep
                                || z == (cz) - indstep
                                || z == (cz) + indstep
                                )
                            {
                                neighbours.Add(new int[] { x, y, z });
                            }
                        }
                    }
                }
            }


            return neighbours;
        }

        public void AssignValuesToCell(int cx, int cy, int cz, double value)
        {
            if (bulge)
            {
                Gdata[cx, cy, cz] += (float)value; // increment the value
            }
            else
            {
                if (value > Gdata[cx, cy, cz])
                {
                    Gdata[cx, cy, cz] = (float)value; // assign the larger value
                }
            }
        }

        public void assignSection(int z)
        {
            if (!useXfm)
            {
                double zVal = box.Z.Min + z * zSpace + box.Center.Z;
                for (int y = 0; y < yRes; y++)
                {
                    double yVal = box.Y.Min + y * ySpace + box.Center.Y;
                    for (int x = 0; x < xRes; x++)
                    {
                        double xVal = box.X.Min + x * xSpace + box.Center.X;
                        double val = AssignValues(xVal, yVal, zVal);
                        Gdata[x, y, z] = (float)val;
                    }
                }
            }
            else
            {
                for (int y = 0; y < yRes; y++)
                {
                    for (int x = 0; x < xRes; x++)
                    {
                        Point3d p = new Point3d(x, y, z);
                        p.Transform(xfm); // transform the box point to world coordinates
                        double val = AssignValues(p.X, p.Y, p.Z);
                        Gdata[x, y, z] = (float)val;
                    }
                }
            }
        }

        public static float[,,] Gdata;

        public double DistanceSq(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);
        }

        public double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(DistanceSq(x1, y1, z1, x2, y2, z2));
        }

        public double AssignValues(double x, double y, double z)
        {
            Sphere searchSphere = new Sphere(new Point3d(x, y, z), range);
            double biggestCharge = 0;
            rTree.Search(searchSphere, (obj, arg) =>
            {
                Point3d p = points[arg.Id];
                double charge = 0;
                if (!linear)
                {
                    charge = (double)values[arg.Id] / (double)DistanceSq(p.X, p.Y, p.Z, x, y, z);
                }
                else
                {
                    charge = (double)values[arg.Id] / (double)Distance(p.X, p.Y, p.Z, x, y, z);
                }
                if (!bulge)
                {
                    if (charge > biggestCharge)
                    {
                        biggestCharge = charge;
                    }
                }
                else
                {
                    biggestCharge += charge;
                }
            });
            return biggestCharge;
        }

        public void CreateEnvironment(double cellSize, out Box box, out int xDim, out int yDim, out int zDim)
        {
            box = new Box(Plane.WorldXY, points);
            box.Inflate(range);
            box.RepositionBasePlane(box.Center);

            xDim = (int)Math.Floor((double)box.X.Length / (double)cellSize);
            yDim = (int)Math.Floor((double)box.Y.Length / (double)cellSize);
            zDim = (int)Math.Floor((double)box.Z.Length / (double)cellSize);

            box.X = new Interval(-(xDim * cellSize) / 2, (xDim * cellSize) / 2);
            box.Y = new Interval(-(yDim * cellSize) / 2, (yDim * cellSize) / 2);
            box.Z = new Interval(-(zDim * cellSize) / 2, (zDim * cellSize) / 2);
        }

        public void CreateEnvironment(double cellSize, Box boxIn, out Box box, out int xDim, out int yDim, out int zDim)
        {
            box = boxIn;
            box.RepositionBasePlane(box.Center);

            xDim = (int)Math.Floor((double)box.X.Length / (double)cellSize);
            yDim = (int)Math.Floor((double)box.Y.Length / (double)cellSize);
            zDim = (int)Math.Floor((double)box.Z.Length / (double)cellSize);

            box.X = new Interval(-(xDim * cellSize) / 2, (xDim * cellSize) / 2);
            box.Y = new Interval(-(yDim * cellSize) / 2, (yDim * cellSize) / 2);
            box.Z = new Interval(-(zDim * cellSize) / 2, (zDim * cellSize) / 2);
        }
    }
}
