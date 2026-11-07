using UnityEngine;

namespace Game.Animals.Core
{
    /// <summary>
    /// Определяет тип команды взаимодействия игрока с животным.
    /// </summary>
    public enum AnimalInteractionType
    {
        Pet = 0
    }

    /// <summary>
    /// Описывает объект животного, который может принять команду взаимодействия.
    /// </summary>
    public interface IAnimalInteractable
    {
        /// <summary>
        /// Пытается выполнить указанное взаимодействие со стороны актора.
        /// </summary>
        bool TryInteract(
            AnimalInteractionType interactionType,
            Transform actor);
    }
}
