using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace System.Management.Automation
{
    public sealed class ParameterMetadata
    {
        public ParameterMetadata(ParameterMetadata other)
        {
            this.Name = other.Name;
            this.ParameterSets = other.ParameterSets;
            this._mandatory = other._mandatory;
            this._position = other._position;
        }

        public ParameterMetadata(string name)
        {
            this.Name = name;
            this.ParameterSets = new Dictionary<string, ParameterSetMetadata>();
        }

        public ParameterMetadata(string name, Type parameterType) : this(name)
        {
            this.ParameterType = parameterType;
        }

        public ParameterMetadata(PropertyInfo info) : this(info.Name, info.PropertyType)
        {
            Debug.Assert(info != null);
            this._property = info;
            var attributes = info.GetCustomAttributes();

            foreach (var attr in _property.GetCustomAttributes())
            {
                _attributes.Add(attr);
                var pAttr = attr as ParameterAttribute;
                if (pAttr != null)
                {
                    SetMandatory(pAttr.ParameterSetName, pAttr.Mandatory);
                    SetPosition(pAttr.ParameterSetName, pAttr.Position);
                }
            }
        }

        public IEnumerable<string> Aliases
        {
            get
            {
                var attributes = _property != null ? _property.GetCustomAttributes() : null;
                return attributes != null ?
                    attributes.Where(a => a is AliasAttribute).Select(a => a as AliasAttribute).SelectMany(a => a.AliasNames) :
                    new string[0];
            }
        }

        public Collection<Attribute> Attributes { get { return _attributes; } }

        public bool IsDynamic { get; set; }

        internal int Position(string parameterSet)
        {
            if (!string.IsNullOrEmpty(parameterSet))
            {
                if (_position.ContainsKey(parameterSet))
                    return _position[parameterSet];
            }
            if (_position.ContainsKey("AllParameterSets"))
                return _position["AllParameterSets"];
            return -1;
        }

        internal void SetPosition(string parameterSet, int position)
        {
            if (position != -1)
            {
                if (string.IsNullOrEmpty(parameterSet))
                    parameterSet = "AllParameterSets";
                _position[parameterSet] = position;
            }
        }

        internal bool IsMandatory(string parameterSet)
        {
            if (!string.IsNullOrEmpty(parameterSet))
            {
                if (_position.ContainsKey(parameterSet))
                    return _mandatory[parameterSet];
            }
            if (_mandatory.ContainsKey("AllParameterSets"))
                return _mandatory["AllParameterSets"];
            return false;
        }

        internal void SetMandatory(string parameterSet, bool isMandatory)
        {
            if (string.IsNullOrEmpty(parameterSet))
                parameterSet = "AllParameterSets";
            _mandatory[parameterSet] = isMandatory;
        }

        public string Name { get; set; }

        public Dictionary<string, ParameterSetMetadata> ParameterSets { get; private set; }

        public Type ParameterType { get; set; }

        public bool SwitchParameter { get { return this.ParameterType.Equals(typeof(SwitchParameter)); } }

        internal bool IsBound { get; set; }

        internal bool IsBuiltin { get; set; }


        private void Validate(object argument)
        {
        }

        private PropertyInfo _property;
        private Collection<Attribute> _attributes = new Collection<Attribute>();
        private Dictionary<string, int> _position = new Dictionary<string, int>();
        private Dictionary<string, bool> _mandatory = new Dictionary<string, bool>();
    }
}
