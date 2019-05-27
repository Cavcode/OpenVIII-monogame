﻿using System;
using FF8.Core;
using FF8.Framework;
using FF8.JSM.Format;

namespace FF8.JSM.Instructions
{
    internal sealed class SUBPARTY : JsmInstruction
    {
        private IJsmExpression _characterId;

        public SUBPARTY(IJsmExpression characterId)
        {
            _characterId = characterId;
        }

        public SUBPARTY(Int32 parameter, IStack<IJsmExpression> stack)
            : this(
                characterId: stack.Pop())
        {
        }

        public override String ToString()
        {
            return $"{nameof(SUBPARTY)}({nameof(_characterId)}: {_characterId})";
        }

        public override void Format(ScriptWriter sw, IScriptFormatterContext formatterContext, IServices services)
        {
            sw.Format(formatterContext, services)
                .StaticType(nameof(IPartyService))
                .Method(nameof(IPartyService.RemovePartyCharacter))
                .Enum<CharacterId>(_characterId)
                .Comment(nameof(SUBPARTY));
        }

        public override IAwaitable TestExecute(IServices services)
        {
            CharacterId characterId = (CharacterId)_characterId.Int32(services);
            ServiceId.Party[services].RemovePartyCharacter(characterId);
            return DummyAwaitable.Instance;
        }
    }
}
