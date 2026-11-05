namespace Game.Paint
{
    /// <summary>
    /// Определяет объект, который умеет принимать попадания краской.
    /// </summary>
    public interface IPaintReceiver
    {
        /// <summary>
        /// Принимает данные столкновения и самостоятельно решает, как сохранить и показать краску.
        /// </summary>
        void ReceivePaint(PaintImpactData impact);
    }
}
