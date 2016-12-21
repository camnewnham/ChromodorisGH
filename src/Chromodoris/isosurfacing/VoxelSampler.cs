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
using KDTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromodoris
{
    class VoxelSampler
    {
        public Box _box;
        public double xSpace;
        public double ySpace;
        public double zSpace;

        public KDTree<int> kdTree;
        public int _xRes;
        public int _yRes;
        public int _zRes;
        public List<Point3d> _points;
        public List<double> _values;
        public double _range;
        public double _rangeSq;
        public bool _bulge = false;
        public bool _linear;

        public Transform xfm;

        public Transform xfmToGrid;
        public Transform xfmFromGrid;
        public Transform scaleInv;
        public bool useXfm = false;


        public VoxelSampler(List<Point3d> points, List<double> values, double cellSize, double range, bool bulge, bool linear)
        {
            _points = points;
            _values = values;
            _range = range;
            _rangeSq = _range * _range;
            _bulge = bulge;
            _linear = linear;
            createEnvironment(cellSize, out _box, out _xRes, out _yRes, out _zRes);
        }

        public VoxelSampler(List<Point3d> points, List<double> values, double cellSize, Box box, double range, bool bulge, bool linear)
        {
            _points = points;
            _values = values;
            _range = range;
            _rangeSq = _range * _range;
            _bulge = bulge;
            _linear = linear;
            createEnvironment(cellSize, box, out _box, out _xRes, out _yRes, out _zRes);

            if (_box.Plane.ZAxis != Vector3d.ZAxis || _box.Plane.YAxis != Vector3d.YAxis || _box.Plane.XAxis != Vector3d.XAxis)
            {
                xfm = GetBoxTransform(_box, _xRes, _yRes, _zRes);
                
                useXfm = true;
            }
            
        }

        public VoxelSampler(List<Point3d> points, List<double> values, Box box, int resX, int resY, int resZ, double range, bool bulge, bool linear)
        {
            _points = points;
            _values = values;
            _range = range;
            _rangeSq = _range * _range;
            _bulge = bulge;
            _linear = linear;
            _box = box;
            _box.RepositionBasePlane(box.Center);
            _xRes = resX;
            _yRes = resY;
            _zRes = resZ;
            if (_box.Plane.ZAxis != Vector3d.ZAxis || _box.Plane.YAxis != Vector3d.YAxis || _box.Plane.XAxis != Vector3d.XAxis)
            {
                xfm = GetBoxTransform(_box, _xRes, _yRes, _zRes);
                useXfm = true;
            }
        }

        public Transform GetBoxTransform(Box box, int x, int y, int z)
        {
            Box gridBox = new Box(Plane.WorldXY, new Interval(0, x), new Interval(0, y), new Interval(0, z));
            gridBox.RepositionBasePlane(gridBox.Center);

            var trans = Transform.PlaneToPlane(gridBox.Plane, box.Plane);
            trans = trans * Transform.Scale(gridBox.Plane, box.X.Length / gridBox.X.Length, box.Y.Length / gridBox.Y.Length, box.Z.Length / gridBox.Z.Length);

            return trans;

        }

        public void init()
        {
            xSpace = (_box.X.Max - _box.X.Min) / (_xRes - 1);
            ySpace = (_box.Y.Max - _box.Y.Min) / (_yRes - 1);
            zSpace = (_box.Z.Max - _box.Z.Min) / (_zRes - 1);

            // fill empty variables

            if (_values.Count == 1)
            {
                var val = _values[0];
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    _values.Add(val);
                }

            }
            else  if (_values.Count < _points.Count)
            {
                for (int i = 0; i < _points.Count; i++)
                {
                    _values.Add(1);
                }
            }

            // make transform from full box to scaled box
            // _box is the big box
            var _gridbox = new Box(Plane.WorldXY, new Interval(0, _xRes-1), new Interval(0, _yRes-1), new Interval(0, _zRes-1));
            _gridbox.RepositionBasePlane(_gridbox.Center);
            xfmToGrid = BoxToBoxTransform(_box, _gridbox);
            xfmFromGrid = BoxToBoxTransform(_gridbox, _box);

            // forward sample tree init
            kdTree = new KDTree<int>(3);
            int ind = 0;
            foreach (Point3d p in _points)
            {
                double[] pos = { p.X, p.Y, p.Z };
                kdTree.AddPoint(pos, ind);
                ind++;
            }


            Gdata = new float[_xRes, _yRes, _zRes];
        }

        public Transform BoxToBoxTransform(Box source, Box target)
        {
            var trans = Transform.PlaneToPlane(source.Plane, target.Plane);
            trans = trans * Transform.Scale(source.Plane, target.X.Length / source.X.Length, target.Y.Length / source.Y.Length, target.Z.Length / source.Z.Length);
            return trans;
        }

        public Box getBox()
        {
            return _box;
        }

        public float[,,] getData()
        {
            return Gdata;
        }

        private void executeSingleThread()
        {
            for (int z = 0; z < _zRes; z++)
            {
                assignSection(z);
            }
        }

        public void executeMultiThreaded()
        {
            var pLel = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.ForEach(Enumerable.Range(0, _zRes), pLel, z => assignSection(z));
        }

        public void executeInverse()
        {
            for (int i=0; i<_points.Count; i++)
            {
                AssignPointValue(i);
            }
        }

        public void executeInverseMultiThreaded()
        {
            var pLel = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.For(0, _points.Count, pLel, i => AssignPointValue(i));
        }

        public void AssignPointValue(int i)
        {
            Point3d p = _points[i];
            // transform to box space
            Point3d ptX = new Point3d(p);
            ptX.Transform(xfmToGrid);

            int[] closeCell = new int[] { (int)ptX.X, (int)ptX.Y, (int)ptX.Z }; // round to find the closest cell
                                                                                // if the point falls outside the box, skip it
            if (ptX.X < 0 || ptX.X >= _xRes || ptX.Y < 0 || ptX.Y >= _yRes || ptX.Z < 0 || ptX.Z >= _zRes)
                return;

            if (assignValueFromScaledPoint(ptX, closeCell, _values[i])) // first (center) value was applied
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
                        if (assignValueFromScaledPoint(ptX, cell, _values[i]))
                        {
                            inprogress = true;
                        }
                    }
                }

                var test = Gdata;
                // by here, all values should have been assigned from this point...
            }
        }

        public bool assignValueFromScaledPoint(Point3d ptX, int[] closeCell, double scalar)
        {
            // get distance to the cell
            Vector3d closestVec = new Point3d(closeCell[0], closeCell[1], closeCell[2]) - ptX;

            // transform vector to world space
            closestVec.Transform(xfmFromGrid);

            if (_linear)
            {
                double len = closestVec.Length;
                if (len > _range) return false;
                // assign a value to this cell
                assignValueToCell(closeCell[0], closeCell[1], closeCell[2], scalar / (len));
                return true;
            }
            else
            {
                double len = closestVec.SquareLength;
                if (len > _rangeSq) return false;
                assignValueToCell(closeCell[0], closeCell[1], closeCell[2], scalar / (len * len));
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
                        if (!(x < 0 || x >= _xRes || y < 0 || y >= _yRes || z < 0 || z >= _zRes))
                        {
                            if (
                                    x == (cx) - indstep
                                ||  x == (cx) + indstep
                                ||  y == (cy) - indstep
                                ||  y == (cy) + indstep
                                ||  z == (cz) - indstep
                                ||  z == (cz) + indstep
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

        public void assignValueToCell(int cx, int cy, int cz, double value)
        {
            if (_bulge) Gdata[cx, cy, cz] += (float) value; // increment the value
            else
            {
                if (value > Gdata[cx, cy, cz])
                {
                    Gdata[cx, cy, cz] = (float) value; // assign the larger value
                }
            }
        }

        public void invAssignValues()
        {


        }

        public void assignSection(int z)
        {
            if (!useXfm)
            {
                double zVal = _box.Z.Min + z * zSpace + _box.Center.Z;
                for (int y = 0; y < _yRes; y++)
                {
                    double yVal = _box.Y.Min + y * ySpace + _box.Center.Y;
                    for (int x = 0; x < _xRes; x++)
                    {
                        double xVal = _box.X.Min + x * xSpace + _box.Center.X;
                        double val = assignValues(xVal, yVal, zVal);
                        Gdata[x, y, z] = (float)val;
                    }
                }
            }
            else
            {
                for (int y = 0; y < _yRes; y++)
                {
                    for (int x = 0; x < _xRes; x++)
                    {
                        Point3d p = new Point3d(x, y, z);
                        p.Transform(xfm); // transform the box point to world coordinates
                        double val = assignValues(p.X, p.Y, p.Z);
                        Gdata[x, y, z] = (float)val;
                    }
                }
            }
        }

        public static float[,,] Gdata;

        public double distanceSquared(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);
        }

        public double distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(distanceSquared(x1, y1, z1, x2, y2, z2));
        }

        public double assignValues(double x, double y, double z)
        {
            double[] pos = { x,y,z };
            var data = kdTree.NearestNeighbors(pos, 1024, _rangeSq);
            double biggestCharge = 0;

            foreach (int ind in data)
            {
                Point3d p = _points[ind];
                double charge = 0;
                if (!_linear)
                {
                    charge = (double)_values[ind] / (double)distanceSquared(p.X, p.Y, p.Z, x, y, z);
                }
                else
                {
                    charge = (double)_values[ind] / (double)distance(p.X, p.Y, p.Z, x, y, z);
                }
                if (!_bulge)
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
            }
            return biggestCharge;
        }

        public void createEnvironment(double cellSize, out Box box, out int xDim, out int yDim, out int zDim)
        {
            box = new Box(Plane.WorldXY, _points);
            box.Inflate(_range);
            box.RepositionBasePlane(box.Center);

            xDim = (int)Math.Floor((double)box.X.Length / (double)cellSize);
            yDim = (int)Math.Floor((double)box.Y.Length / (double)cellSize);
            zDim = (int)Math.Floor((double)box.Z.Length / (double)cellSize);

            double xLen = xDim * cellSize;

            box.X = new Interval(-(xDim * cellSize) / 2, (xDim * cellSize) / 2);
            box.Y = new Interval(-(yDim * cellSize) / 2, (yDim * cellSize) / 2);
            box.Z = new Interval(-(zDim * cellSize) / 2, (zDim * cellSize) / 2);
        }

        public void createEnvironment(double cellSize, Box boxIn, out Box box, out int xDim, out int yDim, out int zDim)
        {
            box = boxIn;
            box.RepositionBasePlane(box.Center);

            xDim = (int)Math.Floor((double)box.X.Length / (double)cellSize);
            yDim = (int)Math.Floor((double)box.Y.Length / (double)cellSize);
            zDim = (int)Math.Floor((double)box.Z.Length / (double)cellSize);

            double xLen = xDim * cellSize;

            box.X = new Interval(-(xDim * cellSize) / 2, (xDim * cellSize) / 2);
            box.Y = new Interval(-(yDim * cellSize) / 2, (yDim * cellSize) / 2);
            box.Z = new Interval(-(zDim * cellSize) / 2, (zDim * cellSize) / 2);
        }
    }
}
