using SignalR.Models;
using System.Collections.Concurrent;

namespace SignalR.Services
{
    /// <summary>
    /// Se utiliza 'ConcurrentDictionary' para almacenar las salas a las que se ha unido cada conexión. 
    /// En el contexto de una aplicación SignalR, 'ConcurrentDictionary' es la elección adecuada porque
    /// SignalR maneja múltiples conexiones concurrentes, lo que significa que múltiples hilos pueden estar accediendo y modificando las colecciones simultáneamente.
    /// Se puede usar 'Dictionary<string, HashSet<string>>' pero no es seguro para hilos y si múltiples hilos intentan modificar el Dictionary simultáneamente, puede ocurrir una excepción.
    /// En resumen, aunque Dictionary es más eficiente en escenarios de un solo hilo, ConcurrentDictionary es la elección correcta para aplicaciones multihilo como servidores web y, en particular, para aplicaciones SignalR donde la concurrencia es un factor crítico.
    /// </summary>
    public class SharedDb
    {
        private readonly ConcurrentDictionary<string, UserConnection> _connections = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _userGroups = [];
        private readonly ConcurrentDictionary<string, HashSet<string>> _groupUsers = [];

        public ConcurrentDictionary<string, UserConnection> Connections => _connections;
        /// <summary>
        /// Almacena la lista de grupos para cada conexión de usuario.
        /// </summary>
        public ConcurrentDictionary<string, HashSet<string>> UserGroups => _userGroups;

        /// <summary>
        /// Almacena la lista de usuarios en cada sala.
        /// </summary>
        public ConcurrentDictionary<string, HashSet<string>> GroupUsers => _groupUsers;
    }
}
