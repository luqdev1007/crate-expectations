using CrateExpectations.Cargo;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// "Тебя заметили". Инспектор оборачивается к прохожему с грузом в руках, пару секунд
    /// смотрит на него и возвращается к обходу. Состояние намеренно ничего не решает и
    /// не открывает случай: пока груз не поставлен на стол, досматривать нечего - это
    /// только взгляд, чтобы пост не выглядел слепым.
    /// Ждём обычным таймером, а не <see cref="Pause"/>: реакция синхронная и короткая,
    /// отменять в ней нечего.
    /// Выход: время вышло - в <see cref="InspectorPhase.Patrol"/>; груз всё-таки поставили -
    /// сразу в <see cref="InspectorPhase.Approach"/>, реакция не должна задерживать досмотр
    /// </summary>
    public sealed class InspectorNoticeState : IState
    {
        private readonly IInspectorContext _context;

        private CargoBox _subject;
        private float _remaining;

        public InspectorNoticeState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            _subject = _context.ApproachingCargo;
            _remaining = _context.Definition.NoticeHoldSeconds;
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

            // Ящик мог уехать в руках за спину или исчезнуть вовсе. Разворот при этом
            // доигрывается до конца по последнему известному направлению: инспектор,
            // дёрнувшийся и замерший на полповорота, выглядит хуже, чем просто медленный
            if (_subject != null)
            {
                _context.Motor.FaceTowards(_subject.transform.position, deltaTime);
            }

            _remaining -= deltaTime;
            if (_remaining > 0f) return;

            _context.GoTo(InspectorPhase.Patrol);
        }

        /// <inheritdoc />
        public void Exit() => _subject = null;
    }
}
