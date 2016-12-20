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
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Chromodoris
{
    public class VolumetricSpace
    {

        public int resX, resY, resZ;
        public int resX1, resY1, resZ1;

        public int sliceRes;

        public int numCells;

        private float[,,] data;

        public VolumetricSpace(float[,,] isoData)
        {
            this.resX = isoData.GetLength(0);
            this.resY = isoData.GetLength(1);
            this.resZ = isoData.GetLength(2);
            resX1 = resX - 1;
            resY1 = resY - 1;
            resZ1 = resZ - 1;
            sliceRes = resX * resY;
            numCells = sliceRes * resZ;
            data = isoData;
        }

        private int clip(int val, int min, int max) {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }

        public double getVoxelAt(int index)
        {
            int xVal=0, yVal=0, zVal=0;

            if (index >= sliceRes)
            {
                zVal = (int)Math.Floor((double)index / sliceRes); // find the z row
                index = index - zVal * sliceRes;
            }

            if (index >= resX)
            {
                yVal = (int)Math.Floor((double)index / resX); // find the z row
                index = index - yVal * resX;
            }

            xVal = index;
            return data[xVal,yVal,zVal];
        }

        public double getVoxelAt(int x, int y, int z)
        {
            return data[x, y, z];
        }
    }
}
