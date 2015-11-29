//
//  Author:
//    Martin Haesemeyer m.haesemeyer@gmail.com
//
//  Copyright (c) 2015, Martin Haesemeyer
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

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