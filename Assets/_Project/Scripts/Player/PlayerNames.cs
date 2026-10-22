namespace EyesOff.Player
{
    public static class PlayerNames
    {
        /// <summary>Host is client 0 -> "Oyuncu 1", the joiner is client 1 -> "Oyuncu 2".</summary>
        public static string For(ulong clientId) => $"Oyuncu {clientId + 1}";
    }
}
