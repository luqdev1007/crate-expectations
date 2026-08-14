using CrateExpectations.Core.StateMachine;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Замах и удар. Три внутренние фазы - полем-перечислением, а не тремя состояниями
    /// машины: снаружи это всё один удар, и намерение стражника от того, заносит он
    /// клинок или доводит его, не меняется. Тот же приём, что <c>Moving</c>/<c>Waiting</c>
    /// внутри <see cref="GuardPatrolState"/>.
    /// <para>
    /// <b>Состояние не переключает себя само.</b> Досчитав доводку, оно лишь снимает
    /// <see cref="IGuardStateContext.IsAttackCommitted"/>, и на следующем тике
    /// <see cref="GuardBrain"/> решает заново - бить ещё раз или догонять. Так решение
    /// остаётся в одном месте: состояние, само выбирающее себе преемника, - это второй
    /// мозг, который рано или поздно разойдётся с первым.
    /// </para>
    /// <para>
    /// <b>Но замах оно повторяет само</b> - фазой <see cref="Phase.Ready"/>. Машина
    /// состояний из <c>Core</c> отсекает переход в то же состояние по ссылке, поэтому
    /// второй <c>Enter</c> подряд не случается никогда: пока мозг отвечает
    /// <c>Attack</c>, состояние остаётся тем же объектом. Без повтора изнутри стражник,
    /// у которого игрок остался в радиусе, доигрывал бы ровно один удар и застывал
    /// навсегда - проверено живьём, см. блок 8 отчёта фазы. Это тот же приём, что
    /// <c>Moving</c>/<c>Waiting</c> в <see cref="GuardPatrolState"/>: состояние
    /// повторяет собственное действие, а решать, быть ли ему вообще, по-прежнему мозгу.
    /// </para>
    /// </summary>
    public sealed class GuardAttackState : IState
    {
        private static readonly int AttackTriggerId = Animator.StringToHash("AttackTrigger");
        private static readonly int AttackIndexId = Animator.StringToHash("AttackIndex");

        private readonly IGuardStateContext _context;

        private Phase _phase;
        private float _timer;

        // Чем замахивались в прошлый раз - чтобы не замахнуться так же снова.
        // Минус единица, а не ноль: до первого удара «предыдущего» нет вовсе,
        // и ноль запретил бы первому удару быть нулевым
        private int _previousIndex = -1;

        public GuardAttackState(IGuardStateContext context) => _context = context;

        /// <summary>Что именно происходит внутри удара</summary>
        private enum Phase
        {
            /// <summary>Замах: клинок заносится, задеть ещё не может</summary>
            Windup,

            /// <summary>Клинок идёт: только здесь ищут попадание и держат гипер-армор</summary>
            Active,

            /// <summary>Доводка: задеть уже нельзя, прервать - снова можно</summary>
            Recovery,

            /// <summary>
            /// Удар доигран, коммит снят, решение отдано мозгу. Если на следующем тике
            /// состояние всё ещё тикают - значит мозг оставил стражника драться,
            /// и замах начинается заново
            /// </summary>
            Ready,
        }

        /// <inheritdoc />
        public void Enter() => StartSwing();

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            // Предыдущий удар доигран, а состояние всё ещё тикают - значит мозг
            // на этом кадре снова ответил Attack. Замахиваемся заново
            if (_phase == Phase.Ready)
            {
                StartSwing();
                return;
            }

            // Довернуться на игрока можно только в замахе. Дальше стражник закоммичен
            // и бьёт туда, куда занёс: доводка за целью через всю атаку означала бы,
            // что от удара нельзя уйти в принципе
            if (_phase == Phase.Windup)
                FacePlayer(deltaTime);

            _timer -= deltaTime;

            if (_timer > 0f)
                return;

            switch (_phase)
            {
                case Phase.Windup:
                    BeginActive();
                    break;

                case Phase.Active:
                    BeginRecovery();
                    break;

                case Phase.Recovery:
                    // Доводка кончилась. Себе преемника не назначаем - снимаем коммит
                    // и отдаём решение мозгу. Оставит драться - на следующем тике
                    // фаза Ready начнёт новый замах
                    _context.IsAttackCommitted = false;
                    _phase = Phase.Ready;
                    break;
            }
        }

        /// <summary>
        /// Начало удара - и первого после входа в состояние, и каждого следующего,
        /// пока мозг держит стражника в драке. Один метод на оба случая намеренно:
        /// два одинаковых начала однажды разъехались бы на одну забытую строчку
        /// </summary>
        private void StartSwing()
        {
            NavMeshAgent agent = _context.Agent;

            // Бьёт с места: шаг во время замаха превратил бы удар в таран
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            int index = PickIndex();

            _context.Animator.SetInteger(AttackIndexId, index);
            _context.Animator.SetTrigger(AttackTriggerId);

            // Коммит взводится ЗДЕСЬ, а не в активной фазе: удар считается начатым
            // с первого кадра замаха, иначе игрок отменял бы его, отходя во время заноса
            _context.IsAttackCommitted = true;
            _context.IsHyperArmored = false;

            _phase = Phase.Windup;
            _timer = _context.Combat.AttackWindup;
        }

        /// <summary>
        /// Оставляет контекст чистым НЕЗАВИСИМО от причины выхода - доиграл удар
        /// или его сбили встречным попаданием в замах. Та же дисциплина, что
        /// у <see cref="GuardPatrolState.Exit"/>, и здесь она важнее: забытый
        /// <see cref="IGuardStateContext.IsHyperArmored"/> сделал бы стражника
        /// неуязвимым навсегда, а забытый коммит - навсегда закоммиченным
        /// </summary>
        public void Exit()
        {
            _context.IsAttackCommitted = false;
            _context.IsHyperArmored = false;

            // Окно детекции закрываем безусловно: прерванный на активной фазе удар
            // иначе продолжил бы искать цель, уже не будучи ударом
            if (_context.Melee != null)
                _context.Melee.EndSwing();
        }

        private void BeginActive()
        {
            _phase = Phase.Active;
            _timer = _context.Combat.AttackActive;

            _context.IsHyperArmored = true;

            if (_context.Melee != null)
                _context.Melee.BeginSwing();
        }

        private void BeginRecovery()
        {
            _phase = Phase.Recovery;
            _timer = _context.Combat.AttackRecovery;

            _context.IsHyperArmored = false;

            if (_context.Melee != null)
                _context.Melee.EndSwing();
        }

        /// <summary>
        /// Разворот на игрока со скоростью самого агента: заводить под это отдельное
        /// число значило бы дать стражнику два разных темпа поворота - один в ходьбе,
        /// другой в бою - и однажды забыть их согласовать
        /// </summary>
        private void FacePlayer(float deltaTime)
        {
            if (_context.Target == null || !_context.Target.Exists)
                return;

            Transform body = _context.Agent.transform;

            Vector3 toPlayer = _context.Target.Position - body.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < 1e-4f)
                return;

            body.rotation = Quaternion.RotateTowards(
                body.rotation,
                Quaternion.LookRotation(toPlayer),
                _context.Agent.angularSpeed * deltaTime);
        }

        /// <summary>
        /// Случайный замах, но не тот же, что в прошлый раз. Сдвигом, а не перевыбором
        /// в цикле: цикл на неудачном сиде крутится лишний раз, а сдвиг даёт равномерное
        /// распределение по оставшимся вариантам за один вызов
        /// </summary>
        private int PickIndex()
        {
            int variants = Mathf.Max(1, _context.Combat.AttackVariants);

            if (variants == 1)
                return 0;

            int index = Random.Range(0, variants - 1);

            if (index >= _previousIndex)
                index++;

            // Первый удар в жизни: предыдущего нет, и сдвиг выше сместил бы выбор зря
            if (_previousIndex < 0)
                index = Random.Range(0, variants);

            _previousIndex = index;

            return index;
        }
    }
}
