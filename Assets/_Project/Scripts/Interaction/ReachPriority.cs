namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Единственное правило, по которому одна кнопка руки выбирает себе занятие.
    /// Вынесено из <see cref="Interactor"/> отдельной чистой функцией не ради красоты:
    /// от него зависит и подсказка на экране, и то, что случится по нажатию, а проверить
    /// его в play mode можно только перебором поз перед станцией с ящиком и без.
    /// <para>
    /// Ни одного типа Unity в сигнатуре - только «есть ли цель» и «на каком расстоянии»,
    /// поэтому правило проверяется таблицей случаев в edit-mode тесте
    /// </para>
    /// </summary>
    public static class ReachPriority
    {
        /// <summary>
        /// Что сделает кнопка руки.
        /// </summary>
        /// <param name="carrying">Груз уже в руках</param>
        /// <param name="canGrab">Под прицелом есть груз, и руки свободны его взять</param>
        /// <param name="grabDistance">Дистанция до этого груза, м</param>
        /// <param name="canInteract">Под прицелом есть станция</param>
        /// <param name="interactableDistance">Дистанция до неё, м</param>
        public static ReachAction Resolve(
            bool carrying,
            bool canGrab, float grabDistance,
            bool canInteract, float interactableDistance)
        {
            // Груз в руках отменяет любой выбор. Иначе станция, попавшая под прицел,
            // отбирала бы у игрока единственный способ положить ношу - и он таскал бы
            // ящик по всему порту, ища место, где ничто не мешает его поставить
            if (carrying)
                return ReachAction.Drop;

            if (!canGrab)
                return canInteract ? ReachAction.Interact : ReachAction.None;

            if (!canInteract)
                return ReachAction.Grab;

            // Оба под прицелом - побеждает ближний: игрок смотрит на то, что видит первым.
            // Ровное расстояние отдаём грузу: он физически стоит перед станцией, и это
            // его коллайдер игрок видит в прицеле
            return grabDistance <= interactableDistance ? ReachAction.Grab : ReachAction.Interact;
        }
    }
}
