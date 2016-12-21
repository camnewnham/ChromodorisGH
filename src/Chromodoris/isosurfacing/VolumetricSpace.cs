/*
* This algorithm is based on Karsten Schmidt's 'toxiclibs' isosurfacer in Java
* https://bitbucket.org/postspectacular/toxiclibs
* Released under the Lesser GPL (LGPL 2.1)
*/

using System;

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
