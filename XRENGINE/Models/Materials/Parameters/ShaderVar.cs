using Extensions;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Vectors;
using XREngine.Rendering.Models.Materials.Shaders.Parameters;
using Color = System.Drawing.Color;

namespace XREngine.Rendering.Models.Materials
{
    public interface IUniformableArray : IUniformable { }
    //public interface IUniformable { }
    public interface IShaderVarOwner : IUniformable { }
    public interface IShaderVarType { }

    public interface IShaderNumericType : IShaderVarType { }
    public interface IShaderBooleanType : IShaderVarType { }
    public interface IShaderMatrixType : IShaderVarType { }
    public interface IShaderSignedIntType : IShaderVarType { }
    public interface IShaderUnsignedIntType : IShaderVarType { }
    public interface IShaderFloatType : IShaderVarType { }
    public interface IShaderDoubleType : IShaderVarType { }

    public interface IShaderNonVectorType : IShaderVarType { }
    public interface IShaderVectorType : IShaderVarType { }
    
    public interface IShaderVector2Type : IShaderVarType { }
    public interface IShaderVector3Type : IShaderVarType { }
    public interface IShaderVector4Type : IShaderVarType { }

    public interface IShaderVectorBoolType : IShaderVarType { }
    public interface IShaderVectorSignedIntType : IShaderVarType { }
    public interface IShaderVectorUnsignedIntType : IShaderVarType { }
    public interface IShaderVectorFloatType : IShaderVarType { }
    public interface IShaderVectorDoubleType : IShaderVarType { }
    
    public interface IShaderNonDecimalType : IShaderVarType { }
    public interface IShaderDecimalType : IShaderVarType { }

    public interface IShaderSignedType : IShaderVarType { }
    public interface IShaderUnsignedType : IShaderVarType { }

    public abstract class ShaderVar : XRBase, IShaderVarOwner, IUniformable
    {
        internal const string CategoryName = "Material Parameter";
        internal const string ValueName = "Value";
        public const string NoName = "NoName";

        public event Action<ShaderVar>? ValueChanged;

        //Determines if this var's components can be moved around
        protected bool _canSwizzle = true;
        protected Dictionary<string, ShaderVar> _fields = [];

        protected IShaderVarOwner? _owner;
        internal IShaderVarOwner? Owner => _owner;

        public abstract EShaderVarType TypeName { get; }

        private string _name = string.Empty;
        [Browsable(true)]
        [Category(CategoryName)]
        [DisplayName("Uniform Name")]
        public string Name
        {
            get => _name;
            set => _name = (value ?? "").ReplaceWhitespace("");
        }

        [Browsable(false)]
        public abstract object GenericValue { get; }

        protected bool _valueChanged = true;
        public void SetUniform(XRRenderProgram program, string? nameOverride = null)
        {
            if (!_valueChanged)
                return;
            
            SetProgramUniform(program, nameOverride ?? Name);
            _valueChanged = false;
        }

        //internal void SetUniform(string name) { SetUniform(Api.GetUniformLocation(programBindingId, name)); }
        //internal void SetUniform() { SetUniform(Api.GetUniformLocation(programBindingId, Name)); }

        protected abstract void SetProgramUniform(XRRenderProgram program, string name);

        public ShaderVar(string userName, IShaderVarOwner? owner)
        {
            _owner = owner;
            Name = userName;
        }

        protected void OnValueChanged()
        {
            _valueChanged = true;
            ValueChanged?.Invoke(this);
        }

        /// <summary>
        /// Ex: layout (location = 0) uniform float potato;
        /// </summary>
        internal string GetUniformDeclaration(int bindingLocation = -1)
        {
            string line = "";
            if (bindingLocation >= 0)
                line = string.Format("layout (location = {0}) ", bindingLocation);
            return line + string.Format("uniform {0};", GetDeclaration());
        }

        internal string GetDeclaration()
            => string.Format("{0} {1}", TypeName.ToString()[1..], Name);

        internal abstract string GetShaderValueString();
        /// <summary>
        /// Ex: this is float '.x', parent is Vector4 '[0]', parent is mat4 'tomato': tomato[0].x
        /// </summary>
        /// <returns></returns>
        internal virtual string AccessorTree()
        {
            return Name;
        }

        internal static Vector4 GetTypeColor(EShaderVarType argumentType)
            => argumentType switch
            {
                EShaderVarType._bool or EShaderVarType._bVector2 or EShaderVarType._bVector3 or EShaderVarType._bVector4 => (ColorF4)Color.Red,
                EShaderVarType._int or EShaderVarType._iVector2 or EShaderVarType._iVector3 or EShaderVarType._iVector4 => (ColorF4)Color.HotPink,
                EShaderVarType._uint or EShaderVarType._uVector2 or EShaderVarType._uVector3 or EShaderVarType._uVector4 => (ColorF4)Color.Orange,
                EShaderVarType._float or EShaderVarType._vector2 or EShaderVarType._vector3 or EShaderVarType._vector4 => (ColorF4)Color.Blue,
                EShaderVarType._double or EShaderVarType._dVector2 or EShaderVarType._dVector3 or EShaderVarType._dVector4 => (ColorF4)Color.Green,
                _ => (ColorF4)Color.Black,
            };

        #region Type caches
        //public static EShaderVarType[] GetTypesMatching<T>() where T : IShaderVarType
        //{
        //    Type varType = typeof(T);
        //    Type shaderType = typeof(ShaderVar);
        //    var types = AppDomainHelper.FindTypes(t => t.IsSubclassOf(shaderType) && varType.IsAssignableFrom(t));
        //    return types.Select(x => TypeAssociations[x]).Distinct().ToArray();
        //}
        public static readonly Dictionary<Type, EShaderVarType> TypeAssociations = new()
        {
            { typeof(ShaderBool),   EShaderVarType._bool   },
            { typeof(ShaderInt),    EShaderVarType._int    },
            { typeof(ShaderUInt),   EShaderVarType._uint   },
            { typeof(ShaderFloat),  EShaderVarType._float  },
            { typeof(ShaderDouble), EShaderVarType._double },
            { typeof(ShaderVector2),   EShaderVarType._vector2   },
            { typeof(ShaderVector3),   EShaderVarType._vector3   },
            { typeof(ShaderVector4),   EShaderVarType._vector4   },
            //{ typeof(ShaderMat3),   EShaderVarType._mat3   },
            { typeof(ShaderMat4),   EShaderVarType._mat4   },
            { typeof(ShaderIVector2),  EShaderVarType._iVector2  },
            { typeof(ShaderIVector3),  EShaderVarType._iVector3  },
            { typeof(ShaderIVector4),  EShaderVarType._iVector4  },
            { typeof(ShaderUVector2),  EShaderVarType._uVector2  },
            { typeof(ShaderUVector3),  EShaderVarType._uVector3  },
            { typeof(ShaderUVector4),  EShaderVarType._uVector4  },
            { typeof(ShaderDVector2),  EShaderVarType._dVector2  },
            { typeof(ShaderDVector3),  EShaderVarType._dVector3  },
            { typeof(ShaderDVector4),  EShaderVarType._dVector4  },
            { typeof(ShaderBVector2),  EShaderVarType._bVector2  },
            { typeof(ShaderBVector3),  EShaderVarType._bVector3  },
            { typeof(ShaderBVector4),  EShaderVarType._bVector4  },
        };
        public static readonly Dictionary<EShaderVarType, Type> ShaderTypeAssociations = new()
        {
            { EShaderVarType._bool,     typeof(ShaderBool)      },
            { EShaderVarType._int,      typeof(ShaderInt)       },
            { EShaderVarType._uint,     typeof(ShaderUInt)      },
            { EShaderVarType._float,    typeof(ShaderFloat)     },
            { EShaderVarType._double,   typeof(ShaderDouble)    },
            { EShaderVarType._vector2,     typeof(ShaderVector2)      },
            { EShaderVarType._vector3,  typeof(ShaderVector3)   },
            { EShaderVarType._vector4,     typeof(ShaderVector4)      },
            //{ EShaderVarType._mat3,     typeof(ShaderMat3)      },
            { EShaderVarType._mat4,     typeof(ShaderMat4)      },
            { EShaderVarType._iVector2,    typeof(ShaderIVector2)     },
            { EShaderVarType._iVector3, typeof(ShaderIVector3)  },
            { EShaderVarType._iVector4,    typeof(ShaderIVector4)     },
            { EShaderVarType._uVector2,    typeof(ShaderUVector2)     },
            { EShaderVarType._uVector3, typeof(ShaderUVector3)  },
            { EShaderVarType._uVector4,    typeof(ShaderUVector4)     },
            { EShaderVarType._dVector2,    typeof(ShaderDVector2)     },
            { EShaderVarType._dVector3, typeof(ShaderDVector3)  },
            { EShaderVarType._dVector4,    typeof(ShaderDVector4)     },
            { EShaderVarType._bVector2,    typeof(ShaderBVector2)     },
            { EShaderVarType._bVector3, typeof(ShaderBVector3)  },
            { EShaderVarType._bVector4,    typeof(ShaderBVector4)     },
        };
        public static readonly Dictionary<EShaderVarType, Type> AssemblyTypeAssociations = new()
        {
            { EShaderVarType._bool,     typeof(bool)        },
            { EShaderVarType._int,      typeof(int)         },
            { EShaderVarType._uint,     typeof(uint)        },
            { EShaderVarType._float,    typeof(float)       },
            { EShaderVarType._double,   typeof(double)      },
            { EShaderVarType._vector2,     typeof(Vector2)     },
            { EShaderVarType._vector3,  typeof(Vector3)     },
            { EShaderVarType._vector4,     typeof(Vector4)     },
            //{ EShaderVarType._mat3,     typeof(Matrix3)     },
            { EShaderVarType._mat4,     typeof(Matrix4x4)   },
            { EShaderVarType._iVector2,    typeof(IVector2)    },
            { EShaderVarType._iVector3, typeof(IVector3)    },
            { EShaderVarType._iVector4,    typeof(IVector4)    },
            { EShaderVarType._uVector2,    typeof(UVector2)    },
            { EShaderVarType._uVector3, typeof(UVector3)    },
            { EShaderVarType._uVector4,    typeof(UVector4)    },
            { EShaderVarType._dVector2,    typeof(DVector2)    },
            { EShaderVarType._dVector3, typeof(DVector3)    },
            { EShaderVarType._dVector4,    typeof(DVector4)    },
            { EShaderVarType._bVector2,    typeof(BoolVector2) },
            { EShaderVarType._bVector3, typeof(BoolVector3) },
            { EShaderVarType._bVector4,    typeof(BoolVector4) },
        };
        public static readonly EShaderVarType[] SignedIntTypes =
        [
            EShaderVarType._int,
            EShaderVarType._iVector2,
            EShaderVarType._iVector3,
            EShaderVarType._iVector4,
        ];
        public static readonly EShaderVarType[] UnsignedIntTypes =
        [
            EShaderVarType._uint,
            EShaderVarType._uVector2,
            EShaderVarType._uVector3,
            EShaderVarType._uVector4,
        ];
        public static readonly EShaderVarType[] IntegerTypes =
        [
            EShaderVarType._int,
            EShaderVarType._uint,
            EShaderVarType._iVector2,
            EShaderVarType._uVector2,
            EShaderVarType._iVector3,
            EShaderVarType._uVector3,
            EShaderVarType._iVector4,
            EShaderVarType._uVector4,
        ];
        public static readonly EShaderVarType[] DecimalTypes =
        [
            EShaderVarType._float,
            EShaderVarType._double,
            EShaderVarType._vector2,
            EShaderVarType._dVector2,
            EShaderVarType._vector3,
            EShaderVarType._dVector3,
            EShaderVarType._vector4,
            EShaderVarType._dVector4,
        ];
        public static readonly EShaderVarType[] FloatTypes =
        [
            EShaderVarType._float,
            EShaderVarType._vector2,
            EShaderVarType._vector3,
            EShaderVarType._vector4,
        ];
        public static readonly EShaderVarType[] DoubleTypes =
        [
            EShaderVarType._double,
            EShaderVarType._dVector2,
            EShaderVarType._dVector3,
            EShaderVarType._dVector4,
        ];
        public static readonly EShaderVarType[] NumericTypes =
        [
            EShaderVarType._float,
            EShaderVarType._double,
            EShaderVarType._int,
            EShaderVarType._uint,
            EShaderVarType._vector2,
            EShaderVarType._iVector2,
            EShaderVarType._uVector2,
            EShaderVarType._dVector2,
            EShaderVarType._vector3,
            EShaderVarType._iVector3,
            EShaderVarType._uVector3,
            EShaderVarType._dVector3,
            EShaderVarType._vector4,
            EShaderVarType._iVector4,
            EShaderVarType._uVector4,
            EShaderVarType._dVector4,
        ];
        public static readonly EShaderVarType[] SignedTypes =
        [
            EShaderVarType._float,
            EShaderVarType._double,
            EShaderVarType._int,
            EShaderVarType._vector2,
            EShaderVarType._iVector2,
            EShaderVarType._dVector2,
            EShaderVarType._vector3,
            EShaderVarType._iVector3,
            EShaderVarType._dVector3,
            EShaderVarType._vector4,
            EShaderVarType._iVector4,
            EShaderVarType._dVector4,
        ];
        public static readonly EShaderVarType[] BooleanTypes =
        [
            EShaderVarType._bool,
            EShaderVarType._bVector2,
            EShaderVarType._bVector3,
            EShaderVarType._bVector4,
        ];
        public static readonly EShaderVarType[] VectorTypes =
        [
            EShaderVarType._vector2,
            EShaderVarType._iVector2,
            EShaderVarType._uVector2,
            EShaderVarType._dVector2,
            EShaderVarType._bVector2,
            EShaderVarType._vector3,
            EShaderVarType._iVector3,
            EShaderVarType._uVector3,
            EShaderVarType._dVector3,
            EShaderVarType._bVector3,
            EShaderVarType._vector4,
            EShaderVarType._iVector4,
            EShaderVarType._uVector4,
            EShaderVarType._dVector4,
            EShaderVarType._bVector4,
        ];
        #endregion
    }

    public enum EShaderVarType
    {
        _bool,
        _int,
        _uint,
        _float,
        _double,
        _vector2,
        _vector3,
        _vector4,
        _mat3,
        _mat4,
        _iVector2,
        _iVector3,
        _iVector4,
        _uVector2,
        _uVector3,
        _uVector4,
        _dVector2,
        _dVector3,
        _dVector4,
        _bVector2,
        _bVector3,
        _bVector4
    }
}
