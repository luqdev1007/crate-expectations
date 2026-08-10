using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Мелочь, нужная всем, кто пишет в <see cref="Animator"/> у игрока: у него два графа,
    /// и параметры у них разные
    /// </summary>
    public static class AnimatorParameters
    {
        /// <summary>
        /// Есть ли у контроллера такой параметр. Единственный способ спросить - перебрать
        /// список: <see cref="Animator"/> проверки по имени не даёт, а запись
        /// в несуществующий параметр он молча не глотает - это варнинг в консоль,
        /// и не один, а по одному на каждую запись.
        /// <para>
        /// Спрашивать полагается один раз, в <c>Awake</c>: обращение к
        /// <c>Animator.parameters</c> отдаёт массив, то есть аллоцирует
        /// </para>
        /// </summary>
        public static bool Has(Animator animator, string parameterName)
        {
            if (animator == null)
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].name == parameterName)
                    return true;

            return false;
        }
    }
}
