using System.Collections.Generic;
using Godot;




// dictionary-based adjacency list
public class MonkMarkers
{
	public Marker3D TopRight { get; }
	public Marker3D TopLeft { get; }
	public Marker3D BottomLeft { get; }
	public Marker3D BottomRight { get; }
	public Marker3D Doorway { get; }
	public Marker3D Inside { get; }
	public Marker3D DeepInside { get; }
	public Marker3D Window { get; }

	private readonly Dictionary<Marker3D, List<Marker3D>> _adj = new();

	public MonkMarkers(
		Marker3D topRight, Marker3D topLeft, Marker3D bottomLeft, Marker3D bottomRight,
		Marker3D doorway, Marker3D inside, Marker3D deepInside,
		Marker3D window)
	{
		TopRight = topRight; TopLeft = topLeft;
		BottomLeft = bottomLeft; BottomRight = bottomRight;
		Doorway = doorway; Inside = inside; DeepInside = deepInside;
		Window = window;

		// hallway ring around the room
		Link(TopRight, TopLeft);
		Link(TopLeft, BottomLeft);
		Link(BottomLeft, BottomRight);
		Link(BottomRight, TopRight);

		// doorway only reachable from the right side of the ring
		Link(TopRight, Doorway);
		Link(BottomRight, Doorway);

		// into the room
		Link(Doorway, Inside);
		Link(Inside, DeepInside);

		// window only reachable from left
		Link(TopLeft, Window);
		Link(BottomLeft, Window);
	}

	// undirected edge: add each node to the other's neighbor list
	private void Link(Marker3D a, Marker3D b)
	{
		AddDirected(a, b);
		AddDirected(b, a);
	}

	private void AddDirected(Marker3D from, Marker3D to)
	{
		if (!_adj.TryGetValue(from, out var list))
			_adj[from] = list = new List<Marker3D>();
		list.Add(to);
	}

	public IReadOnlyList<Marker3D> Neighbors(Marker3D m) =>
		m != null && _adj.TryGetValue(m, out var list) ? list : System.Array.Empty<Marker3D>();

	// shortest list of markers from 'from' to 'to' (excludes 'from'). Empty if unreachable.
	public List<Marker3D> FindPath(Marker3D from, Marker3D to)
	{
		var path = new List<Marker3D>();
		if (from == null || to == null || from == to) return path;

		var cameFrom = new Dictionary<Marker3D, Marker3D> { [from] = null };
		var queue = new Queue<Marker3D>();
		queue.Enqueue(from);

		while (queue.Count > 0)
		{
			var cur = queue.Dequeue();
			if (cur == to) break;
			foreach (var n in Neighbors(cur))
			{
				if (cameFrom.ContainsKey(n)) continue;
				cameFrom[n] = cur;
				queue.Enqueue(n);
			}
		}

		if (!cameFrom.ContainsKey(to)) return path;
		for (var c = to; c != from; c = cameFrom[c]) path.Add(c);
		path.Reverse();
		return path;
	}
}