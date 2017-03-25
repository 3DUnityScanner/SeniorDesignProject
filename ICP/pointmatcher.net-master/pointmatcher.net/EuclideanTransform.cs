using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using UnityEngine;

namespace pointmatcher.net
{   
    public struct EuclideanTransform
    {
        public Quaternion rotation;
        public Vector3 translation;

        public Vector3 Apply(Vector3 v)
        {
            return (this.rotation * v + this.translation);
        }

        public EuclideanTransform Inverse()
        {
            EuclideanTransform result;
            // the rotation is the opposite of the applied rotation
            Quaternion q = new Quaternion(w: this.rotation.w, x: -1*this.rotation.x, y: -1*this.rotation.y, z: -1*this.rotation.z);
            result.rotation = q;//TODO: get the conjugate CORRECTLY
            result.translation = (result.rotation * (this.translation  * - 1) );
            return result;
        }

        /// p2 = r * p + t
        /// p = (p2 - t0) * r^-1
        /// p = r^-1 * p2 - t * r^-1

        /// <summary>
        /// Computes a transform that represents applying e2 then e1
        /// </summary>
        public static EuclideanTransform operator *(EuclideanTransform e1, EuclideanTransform e2)
        {
            EuclideanTransform result;
            result.rotation = e1.rotation * e2.rotation;
            result.translation = (e1.rotation * e2.translation) + e1.translation;//TODO originally transform(translation, rotation)
            return result;
        }

        public static EuclideanTransform Identity
        {
            get
            {
                return new EuclideanTransform
                {
                    translation = new Vector3(),
                    rotation = Quaternion.identity
                };
            }
        }
    }
}
