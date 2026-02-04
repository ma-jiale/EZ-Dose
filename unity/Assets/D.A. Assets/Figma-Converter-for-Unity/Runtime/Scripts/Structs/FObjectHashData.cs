using DA_Assets.FCU.Model;
using System.Collections.Generic;

namespace DA_Assets.FCU
{
    public struct FObjectHashData
    {
        private int indent;
        private FObject fobject;
        private List<FieldHashData> hashes;
        public List<EffectHashData> effectDatas;

        public FObjectHashData(FObject fobject, List<FieldHashData> hashes, List<EffectHashData> effectDatas, int indent)
        {
            this.fobject = fobject;
            this.hashes = hashes;
            this.indent = indent;
            this.effectDatas = effectDatas;
        }

        public int Indent => indent;
        public FObject FObject => fobject;
        public List<FieldHashData> FieldHashes => hashes;
        public List<EffectHashData> EffectDatas => effectDatas;
    }
}