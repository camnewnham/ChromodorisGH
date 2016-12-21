/*
* This algorithm is based on Karsten Schmidt's 'toxiclibs' isosurfacer in Java
* https://bitbucket.org/postspectacular/toxiclibs
* Released under the Lesser GPL (LGPL 2.1)
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Chromodoris
{
    public class HashIsoSurface
    {

        VolumetricSpace volume;

        public double isoValue;

        int resX, resY, resZ;
        int resX1, resY1, resZ1;

        int sliceRes;
        int nextXY;

        Dictionary<int, int> edgeVertices; // int2 is face index

        short[] cellIndexCache, prevCellIndexCache;

        public HashIsoSurface(VolumetricSpace volume)
        {
            this.volume = volume;

            resX = volume.resX;
            resY = volume.resY;
            resZ = volume.resZ;
            resX1 = volume.resX1;
            resY1 = volume.resY1;
            resZ1 = volume.resZ1;

            sliceRes = volume.sliceRes;
            nextXY = resX + sliceRes;

            cellIndexCache = new short[sliceRes];
            prevCellIndexCache = new short[sliceRes];

            reset();
        }


        public Mesh computeSurfaceMesh(double iso, ref Rhino.Geometry.Mesh mesh)
        {
            isoValue = iso;

            double offsetZ = 0;
            for (int z = 0; z < resZ1; z++)
            {
                int sliceOffset = sliceRes * z;
                double offsetY = 0;
                for (int y = 0; y < resY1; y++)
                {
                    double offsetX = 0;
                    int sliceIndex = resX * y;
                    int offset = sliceIndex + sliceOffset;
                    for (int x = 0; x < resX1; x++)
                    {
                        int cellIndex = getCellIndex(x, y, z);
                        cellIndexCache[sliceIndex + x] = (short)cellIndex;
                        if (cellIndex > 0 && cellIndex < 255)
                        {
                            int edgeFlags = MarchingCubesIndex.edgesToCompute[cellIndex];
                            if (edgeFlags > 0 && edgeFlags < 255)
                            {
                                int edgeOffsetIndex = offset * 3;
                                double offsetData = volume.getVoxelAt(offset);
                                double isoDiff = isoValue - offsetData;
                                if ((edgeFlags & 1) > 0)
                                {
                                    double t = isoDiff  / (volume.getVoxelAt(offset + 1) - offsetData);
                                    edgeVertices[edgeOffsetIndex] = mesh.Vertices.Add(offsetX + t, y, z);
                                }

                                if ((edgeFlags & 2) > 0)
                                {
                                    double t = isoDiff
                                            / (volume.getVoxelAt(offset + resX) - offsetData);
                                    edgeVertices[edgeOffsetIndex + 1] = mesh.Vertices.Add(x, offsetY + t
                                                    , z
                                                    );
                                }

                                if ((edgeFlags & 4) > 0)
                                {
                                    double t = isoDiff
                                            / (volume.getVoxelAt(offset + sliceRes) - offsetData);
                                    edgeVertices[edgeOffsetIndex + 2] =
                                            mesh.Vertices.Add(x, y,
                                                    offsetZ + t);
                                }
                            }
                        }
                        offsetX++;
                        offset++;
                    }
                    offsetY++;
                }
                if (z > 0)
                {
                    createFacesForSlice(mesh, z - 1);
                }
                short[] tmp = prevCellIndexCache;
                prevCellIndexCache = cellIndexCache;
                cellIndexCache = tmp;
                offsetZ ++;
            }
            createFacesForSlice(mesh, resZ1 - 1);
            return mesh;
        }

        private void createFacesForSlice(Rhino.Geometry.Mesh mesh, int z)
        {
            int[] face = new int[16];
            int sliceOffset = sliceRes * z;
            for (int y = 0; y < resY1; y++)
            {
                int offset = resX * y;
                for (int x = 0; x < resX1; x++)
                {
                    int cellIndex = prevCellIndexCache[offset];
                    if (cellIndex > 0 && cellIndex < 255)
                    {
                        int n = 0;
                        int edgeIndex;
                        int[] cellTriangles = MarchingCubesIndex.cellTriangles[cellIndex];
                        while ((edgeIndex = cellTriangles[n]) != -1)
                        {
                            int[] edgeOffsetInfo = MarchingCubesIndex.edgeOffsets[edgeIndex];
                            face[n] = ((x + edgeOffsetInfo[0]) + resX
                                    * (y + edgeOffsetInfo[1]) + sliceRes
                                    * (z + edgeOffsetInfo[2]))
                                    * 3 + edgeOffsetInfo[3];
                            n++;
                        }
                        for (int i = 0; i < n; i += 3)
                        {
                            try
                            {
                                int va = edgeVertices[face[i + 1]];
                                int vb = edgeVertices[face[i + 2]];
                                int vc = edgeVertices[face[i]];
                                mesh.Faces.AddFace(vc, vb, va);
                            }
                            catch { };
                        }
                    }
                    offset++;
                }
            }
            int minIndex = sliceOffset * 3;


            List<int> toRemove = new List<int>();
            foreach (KeyValuePair<int, int> entry in edgeVertices)
            {
                if (entry.Key < minIndex)
                {
                    toRemove.Add(entry.Key);
                }
            }
            foreach (int dat in toRemove)
            {
                edgeVertices.Remove(dat);
            }

            /*
            for (Iterator<Entry<int, Vector3d>> i = edgeVertices.entrySet().iterator(); i.hasNext();)
            {
                if (i.next().getKey() < minIndex)
                {
                    i.remove();
                }
            }
            */
        }

        protected int getCellIndex(int x, int y, int z)
        {
            int cellIndex = 0;
            int idx = x + y * resX + z * sliceRes;
            if (volume.getVoxelAt(idx) < isoValue)
            {
                cellIndex |= 0x01;
            }
            if (volume.getVoxelAt(idx + sliceRes) < isoValue)
            {
                cellIndex |= 0x08;
            }
            if (volume.getVoxelAt(idx + resX) < isoValue)
            {
                cellIndex |= 0x10;
            }
            if (volume.getVoxelAt(idx + resX + sliceRes) < isoValue)
            {
                cellIndex |= 0x80;
            }
            idx++;
            if (volume.getVoxelAt(idx) < isoValue)
            {
                cellIndex |= 0x02;
            }
            if (volume.getVoxelAt(idx + sliceRes) < isoValue)
            {
                cellIndex |= 0x04;
            }
            if (volume.getVoxelAt(idx + resX) < isoValue)
            {
                cellIndex |= 0x20;
            }
            if (volume.getVoxelAt(idx + resX + sliceRes) < isoValue)
            {
                cellIndex |= 0x40;
            }
            return cellIndex;
        }

        /**
         * Resets mesh vertices to default positions and clears face index. Needs to
         * be called inbetween successive calls to
         * {@link #computeSurfaceMesh(Mesh, double)}.
         */
        public void reset()
        {
            edgeVertices = new Dictionary<int, int>(
                    (int)(resX * resY * 10));
        }
    }
}
