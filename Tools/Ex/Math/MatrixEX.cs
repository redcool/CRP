using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities.CRP
{
    public static class MatrixEx
    {

        public static ref Matrix4x4 Matrix(ref this Matrix4x4 m,float[,] nums)
        {
            if(nums.GetLength(0) != 4)
                throw new ArgumentException("nums length != 4");

            //var m = new Matrix4x4();

            for (int i = 0; i < nums.GetLength(0); i++)
            {
                var v = new Vector4();
                for (int j = 0; j < nums.GetLength(1); j++)
                {
                    v[j] = nums[i, j];
                }
                m.SetRow(i, v);
            }

            return ref m;
        }

        public static ref Matrix4x4 Matrix(ref this Matrix4x4 m,params float[] nums)
        {
            if (nums.GetLength(0) != 16)
                throw new ArgumentException("nums length != 16");

            var count = 4;

            for (int i = 0; i < count; i++)
            {
                var v = new Vector4();
                for (int j = 0; j < count; j++)
                {
                    v[j] = nums[i * count + j];
                }
                m.SetRow(i, v);
            }

            return ref m;
        }

        public static Matrix4x4 Matrix(params float[] nums)
        {
            var m = new Matrix4x4();
            m.Matrix(nums);
            return m;
        }
    }
}
