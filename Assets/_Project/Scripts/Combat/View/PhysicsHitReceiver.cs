using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Реакция физического тела на удар: отлететь. Единственный видимый эффект попадания
    /// по ящику - и главный, ради которого удар вообще чувствуется.
    /// <para>
    /// Здоровья у ящика нет, поэтому урон из <see cref="HitInfo"/> он молча игнорирует.
    /// Это не недоделка: <see cref="IDamageable"/> - общий контракт, а реагировать на удар
    /// каждый волен по-своему, и "получил урон" вовсе не обязано означать "стал ближе
    /// к смерти". Появится у груза прочность - она встанет сюда же и урон пойдёт в дело.
    /// </para>
    /// <para>
    /// Сила прикладывается именно в точку касания, а не в центр масс: ящик, задетый
    /// по краю, должен закрутиться. Удар в центр даёт ровный полёт без вращения и
    /// читается как толчок, а не как рубящий удар.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsHitReceiver : MonoBehaviour, IDamageable
    {
        // Наивное "толкать строго туда, куда шёл клинок" на доке не работает, и это
        // видно с первого удара: по ящику под ногами бьют СВЕРХУ ВНИЗ, а вниз ящику
        // деваться некуда - там палуба. Весь импульс уходит в пол, ящик вздрагивает
        // и остаётся на месте. Поэтому направление удара сначала заваливается
        // в горизонталь, и только потом к нему добавляется подброс
        [Tooltip("Насколько завалить удар в горизонталь. 0 - толкаем строго вдоль клинка " +
                 "(удар сверху вбивает цель в пол), 1 - только горизонтальная составляющая. " +
                 "Промежуточные значения оставляют от вертикали столько, чтобы рубящий " +
                 "удар всё ещё читался как рубящий")]
        [SerializeField][Range(0f, 1f)] private float _horizontalBias = 0.85f;

        [Tooltip("Насколько удар подбрасывает цель вверх. Ноль - ящик едет по полу и " +
                 "тормозит о него; небольшая добавка отрывает его от досок, и полёт " +
                 "становится видно")]
        [SerializeField][Range(0f, 1f)] private float _lift = 0.35f;

        private Rigidbody _body;

        private void Awake() => _body = GetComponent<Rigidbody>();

        /// <inheritdoc />
        public void TakeHit(HitInfo hit)
        {
            // Кинематическому телу импульс не передать - Unity его просто проигнорирует,
            // а мы бы этого не заметили. Ящик в руках игрока кинематичен, и попадание
            // по нему должно остаться без последствий, а не молча потеряться
            if (_body == null || _body.isKinematic)
                return;

            _body.AddForceAtPosition(PushDirection(hit.Direction) * hit.Impulse, hit.Point, ForceMode.Impulse);
        }

        private Vector3 PushDirection(Vector3 bladeDirection)
        {
            Vector3 flat = Vector3.ProjectOnPlane(bladeDirection, Vector3.up);

            // Удар строго вертикально - горизонтали в нём нет вовсе, и заваливать нечего.
            // Нормализация нулевого вектора дала бы ноль, и ящик не сдвинулся бы совсем
            Vector3 aimed = flat.sqrMagnitude > 1e-4f
                ? Vector3.Slerp(bladeDirection.normalized, flat.normalized, _horizontalBias)
                : bladeDirection.normalized;

            return (aimed + Vector3.up * _lift).normalized;
        }
    }
}
