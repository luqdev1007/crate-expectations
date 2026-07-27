using System;
using System.Collections.Generic;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Раздаёт одну и ту же реплику нескольким слушателям: пузырю над головой инспектора
    /// и плашке досмотра в HUD. Текст по-прежнему выбирают состояния FSM, здесь он только ветвится.
    /// </summary>
    public sealed class CompositeInspectorVoice : IInspectorVoice
    {
        private readonly IInspectorVoice[] _sinks;

        public CompositeInspectorVoice(params IInspectorVoice[] sinks)
        {
            if (sinks == null)
                throw new ArgumentNullException(nameof(sinks));

            _sinks = Compact(sinks);
        }

        public void Say(string line)
        {
            for (int i = 0; i < _sinks.Length; i++)
                _sinks[i].Say(line);
        }

        public void ShowVerdict(in VerdictReport report)
        {
            for (int i = 0; i < _sinks.Length; i++)
                _sinks[i].ShowVerdict(report);
        }

        public void Clear()
        {
            for (int i = 0; i < _sinks.Length; i++)
                _sinks[i].Clear();
        }

        private static IInspectorVoice[] Compact(IInspectorVoice[] sinks)
        {
            var kept = new List<IInspectorVoice>(sinks.Length);

            for (int i = 0; i < sinks.Length; i++)
                if (sinks[i] != null)
                    kept.Add(sinks[i]);

            return kept.ToArray();
        }
    }
}
