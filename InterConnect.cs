//
// InterConnect.cs
//
// Author:
//       Martin Haesemeyer <m.haesemeyer@gmail.com>
//
// Copyright (c) 2015 Martin Haesemeyer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;

namespace PythonInterface
{
    public interface PyObject<T>
    {
        T Data{ get;}

        string VarName{ get;}

        string EncodingStatement{get;}
    }

    public struct PyScalar<T> : PyObject<T> where T : struct, IComparable, IComparable<T>
    {
        private static int lastID = 0;

        public T Data{ get; private set; }

        public string VarName{ get; private set; }

        public string EncodingStatement
        {
            get
            {
                return string.Format("{0} = {1}", VarName, Data);
            }
        }

        public override string ToString()
        {
            return EncodingStatement;
        }

        public PyScalar(T value, string varName="")
        {
            if (varName == "")
            {
                VarName = "x" + lastID.ToString();
                lastID++;
            }
            else
                VarName = varName;
            Data = value;
        }
    }//PyScalar<T>


    /// <summary>
    /// Represents a Numpy array as a python statement
    /// </summary>
    public struct NumpyArray1D : PyObject<double[]>
    {
        private static int lastID = 0;

        public double[] Data{ get; private set; }

        /// <summary>
        /// The name of the numpy variable
        /// </summary>
        public string VarName{get;private set;}

        public string EncodingStatement
        {
            get
            {
                //TODO: It is unclear what the maximum length statement
                //would be that we can "send over the wire" - have tested
                //arrays with 5e6 elements which worked fine but a better
                //approac might be to return a string[] of multiple statement
                //i.e. appends for long arrays!
                //In that case we will have to decide on what to do with
                //the ToString() override
                if (Data == null || Data.Length == 0)
                    return "";
                StringBuilder sd = new StringBuilder(Data.Length * 2 + 10);
                sd.Append(VarName);
                sd.Append('=');
                sd.Append(string.Format("{0}.array([",PyPlotInterface.NP));
                //add data to statement
                foreach (double d in Data)
                {
                    sd.Append(d.ToString() + ",");
                }
                sd.Append("])");//close list bracket and array function bracket
                return sd.ToString();
            }
        }

        public override string ToString()
        {
            return EncodingStatement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonProcess.NumpyArray1D"/> struct.
        /// </summary>
        /// <param name="data">The data in the array</param>
        /// <param name="varName">The name of the numpy variable</param>
        public NumpyArray1D(double[] data, string varName = "")
        {
            if (varName == "")
            {
                VarName = "x" + lastID.ToString();
                lastID++;
            }
            else
                VarName = varName;
            Data = data;
        }
    }//NumpyArray1D

}