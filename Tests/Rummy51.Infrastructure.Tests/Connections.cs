using Rummy51.Application.Lobby;
using Rummy51.Infrastructure.Lobby;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Who is online, which is two views of one fact kept in step. The whole of
/// it is memory, because every connection dies with the process anyway.
/// </summary>
public class Connections
{
    [Fact]
    public void A_first_connection_displaces_nothing()
    {
        var connections = new InMemoryPlayerConnections();

        Assert.Null(connections.Register(Opened.Somebody(), "one"));
    }

    [Fact]
    public void A_second_one_displaces_the_first_and_says_so()
    {
        var connections = new InMemoryPlayerConnections();
        var player = Opened.Somebody();

        connections.Register(player, "one");

        Assert.Equal("one", connections.Register(player, "two"));
    }

    [Fact]
    public void A_player_is_online_once_however_many_pages_they_opened()
    {
        var connections = new InMemoryPlayerConnections();
        var player = Opened.Somebody();

        connections.Register(player, "one");
        connections.Register(player, "two");

        Assert.Single(connections.Online());
    }

    [Fact]
    public void A_connection_dropping_takes_its_player_with_it()
    {
        var connections = new InMemoryPlayerConnections();
        var player = Opened.Somebody();

        connections.Register(player, "one");

        Assert.Equal(player.PlayerId, connections.Remove("one")?.PlayerId);
        Assert.Empty(connections.Online());
    }

    /// <summary>
    /// A displaced connection reports its disconnect after the new one has
    /// already claimed the player. Saying the player left then would be
    /// saying it of somebody who is still with us on a newer page.
    /// </summary>
    [Fact]
    public void But_the_one_it_displaced_dropping_afterwards_takes_nobody()
    {
        var connections = new InMemoryPlayerConnections();
        var player = Opened.Somebody();

        connections.Register(player, "one");
        connections.Register(player, "two");

        Assert.Null(connections.Remove("one"));
        Assert.Single(connections.Online());
    }

    [Fact]
    public void A_connection_nobody_registered_takes_nobody_either()
    {
        var connections = new InMemoryPlayerConnections();

        Assert.Null(connections.Remove("one"));
    }

    /// <summary>
    /// Ordinal, not the current culture: the server's locale must not
    /// decide what order Hebrew and Latin names appear in.
    /// </summary>
    [Fact]
    public void The_roster_is_ordered_the_same_way_whatever_the_server_reads_in()
    {
        var connections = new InMemoryPlayerConnections();

        foreach (var name in (string[])["ציון", "Alice", "בני", "bob"])
        {
            connections.Register(Opened.Somebody() with { DisplayName = name }, name);
        }

        Assert.Equal(
            ["Alice", "bob", "בני", "ציון"],
            connections.Online().Select(player => player.DisplayName));
    }

    [Fact]
    public void Holding_a_connection_is_all_that_can_be_observed_so_far()
    {
        var connections = new InMemoryPlayerConnections();

        connections.Register(Opened.Somebody(), "one");

        Assert.Equal(PlayerStatus.Available, connections.Online()[0].Status);
    }
}
