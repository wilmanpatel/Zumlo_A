
using System.Collections.Concurrent;
using Zumlo.Gateway.Domain;

namespace Zumlo.Gateway.Infrastructure
{
    public interface ISessionStore
    {
        void Save(Session session);
        Session? Get(string id);
        SessionSummary GetSummary(string id);
    }

    public class InMemorySessionStore : ISessionStore
    {
        private readonly ConcurrentDictionary<string, Session> _sessions = new();

        public Session? Get(string id) => _sessions.TryGetValue(id, out var s) ? s : null;

        public void Save(Session session) => _sessions[session.Id] = session;

        public SessionSummary GetSummary(string id)
        {
            var s = Get(id) ?? throw new KeyNotFoundException("Session not found");
            var transcript = string.Join(' ', s.Segments.Select(x => x.Text));
            var themes = DeriveThemes(transcript);
            var actions = RecommendActions(transcript);
            return new SessionSummary { SessionId = id, Transcript = transcript, Themes = themes, MicroActions = actions };
        }

        private static List<string> DeriveThemes(string text)
        {
            var themes = new List<string>();
            var lower = text.ToLowerInvariant();
            if (lower.Contains("stress") || lower.Contains("anx")) themes.Add("Stress & Anxiety");
            if (lower.Contains("sleep")) themes.Add("Sleep");
            if (lower.Contains("work")) themes.Add("Workload");
            if (themes.Count == 0) themes.AddRange(new[]{"Mood Check-In","Daily Reflection","Self-care"});
            return themes.Take(3).ToList();
        }

        private static List<string> RecommendActions(string text)
        {
            var actions = new List<string>();
            var lower = text.ToLowerInvariant();
            if (lower.Contains("sleep")) actions.Add("Try a 10-minute wind-down before bed.");
            if (lower.Contains("stress") || lower.Contains("anx")) actions.Add("Do a 1-minute box breathing exercise.");
            if (actions.Count == 0) actions.Add("Take a short walk and hydrate.");
            return actions.Take(2).ToList();
        }
    }
}
