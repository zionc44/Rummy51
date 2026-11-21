# Putting it somewhere

Where this could run, what that costs, and what has to be true first. It
is written down because it came out of conversation and nothing else holds
it.

Prices marked *read* were taken in November 2026 from the provider's own
ordering page. Everything else came from comparison blogs - and the one
blog figure that was later checked against a console was out by a factor
of nearly three, which is how the recommendation here came to change. Treat
an unread figure as something to check and not as a number.

## What decides it

```csharp
services.AddSingleton<ITables, InMemoryTables>();
```

Tables live in memory, and three things follow that nothing else overrides.

**One instance.** Two would be two worlds, and a player who reached the
second would not see the table they opened in the first. So no autoscaling,
no load balancer, no blue-green deployment.

**Always on.** Anything that sleeps when idle kills the SignalR connection
and every table with it. That rules out the free tiers that spin down.

**Every deployment empties the room.** Restarting loses every table.

Three things make it easier in exchange. Angular is served from the
server's own `wwwroot`, so one process serves everything and there is no
CDN and no CORS. Avatars live in a table rather than in files, so there is
nothing to store objects in. And one instance means SignalR needs no
backplane, which would otherwise be another thing to pay for and run.

What all of that adds up to is **one small machine that stays on, with
PostgreSQL beside it**.

It is also not the only thing that machine will carry. A second
application is going beside this one - an appointment book, multi-tenant
from the first table even while it has one tenant - and it will hold other
businesses' customers. That does not change the size of the machine. It
changes which of the paragraphs below are optional, and the one on backups
stops being.

## Contabo, which is the answer

An ordinary virtual machine in Germany - not a managed service. You pick a
Linux image, you get SSH and root, and everything above that is yours.

| plan, read | vCPU | RAM | disk | monthly | 12 months | 24 months |
|---|---|---|---|---|---|---|
| **Cloud VPS 4** | 4 | 8 GB | 100 GB SSD | $6.60 | **$5.61** | $5.28 |
| Cloud VPS 6 | 6 | 12 GB | 200 GB SSD *or* 100 GB NVMe | $9.00 | $7.65 | $7.20 |

Monthly billing adds a one-off $2.40 to set up; the terms do not. Traffic
is "unlimited" with an asterisk, and the network port is 200 Mbit on VPS 4
and 300 Mbit on VPS 6 - which is the speed of the card and not a monthly
allowance, and is some hundreds of times more than anything here will ask
of it.

**Take Cloud VPS 4, monthly at first.** The term saves about $12 a year,
and the first month or two is when you find out whether a provider you have
never used annoys you. Commit once it has not.

Four cores and 8 GB leave room for both applications, Postgres, and a test
environment beside them if you ever want one - which 4 GB would not have.
What pushes you up a plan is memory and not CPU: a card game spends most of
its time waiting for somebody to move, and an appointment book spends it
waiting for somebody to open it. The sign to resize is swap in use or a
container killed for running out of it, not a feeling that something is
slow.

The `/` on VPS 6 is a choice and not a total: 200 GB of SATA SSD, or 100 GB
of NVMe at the same price. NVMe is the one to take when the capacity is
irrelevant either way, because small random writes are what a database
does and latency is the only axis that could ever matter here.

**Snapshots are not backups.** One or two is how many images of the whole
disk you may keep. They live on the same infrastructure, so they protect
you from your own mistakes and not from the machine dying, and a snapshot
of a running Postgres is a power cut rather than a `pg_dump`. Take one
before an operating system upgrade and nothing else.

**What Contabo is known for is oversubscription.** The cores are shared,
and the provider sells more of them than it has, betting that most
customers idle. When the bet is wrong you feel it as *steal time* - the
share of the moments your vCPU wanted to run and the host gave the core to
somebody else. For a build server that would be the end of the argument.
Here it is nearly irrelevant: measured CPU demand is a percent or two, and
the only real burst in the whole system is re-encoding an uploaded avatar.

### Hetzner, which was the answer and is not

The plan this document first recommended, CX23 at about $7, **cannot be
bought.** Hetzner's own page marks the whole Cost-Optimized line, and the
Arm CAX line with it, *Currently not available*, and has done since at
least October 2026. What can be ordered there is CPX22 at **$20.09 read**,
for two cores and 4 GB.

That is three times Contabo's price for half the machine. Nothing else
about Hetzner is worse - 20 TB of traffic stated rather than asterisked, a
gigabit port, hourly billing, a better reputation for the shared cores -
and none of it is worth six times the value. **It is out on price.**

Worth re-reading if the CX line ever comes back, which is also why its
numbers are left here.

### The others, unread

| | per month, claimed |
|---|---|
| Netcup VPS 500 | **€8.26 read**, 2 cores, 4 GB, on a 12-month term |
| Oracle Always Free | **free, read**: about 2 Arm cores and 12 GB, running always |
| Railway | $10-15, double for two apps; no OS to maintain |
| AWS Lightsail | $10-12 |
| GCP, Postgres self-hosted | $17-20; the free e2-micro is US-only with 1 GB egress |
| AWS EC2 + RDS | $28-32, plus $3.60 for a public IPv4 |
| Azure | $27-32 |

**Netcup** is the respectable one - a better name than Contabo for shared
cores - but it wants twelve months before you have run anything, and it is
half a machine for more money.

**Oracle's free tier is genuinely larger than what is bought here**: 1,500
Arm OCPU hours and 9,000 GB hours a month come to roughly two cores and 12
GB running continuously, for ever, and .NET is happy on Arm64. Three
things keep it out. Ampere capacity is often unobtainable in the popular
regions, a Free Tier account may have idle resources reclaimed, and the
sign-up is notoriously hostile. Upgrading to Pay As You Go removes the
reclamation and still costs nothing inside the allowances, and that is the
form to take it in if it is ever taken. For an application another
business depends on, "free but may be reclaimed" is the wrong trade.

**AWS and GCP** are built around autoscaling, several instances and
multi-AZ - every one of which this architecture forbids. You would pay for
a platform and use it as a single server. Their honest reasons are learning
the platform, or already having credits. Cloud Run and App Runner do not
fit at all: they sleep, and they run several instances.

## The domain

One `.com` is about **$11.17 a year** at Cloudflare Registrar, which sells
at cost. Porkbun is a little above it. Avoid the registrars that discount
the first year and renew at $18 and up.

`.co.il` is **dearer than `.com`**, and dearer at Israeli registrars than
`.com` is at the same registrars. There is no price argument for it.

One domain covers any number of applications through subdomains, and Caddy
serves as many as you point at it for nothing. Two domains are worth the
second $11 when the two applications have nothing to do with each other and
one of them is a business whose domain is part of its name.

## What goes on the machine

All of it free, and none of it licensed.

| | |
|---|---|
| Docker and Compose | runs everything |
| **Caddy** | reverse proxy, and TLS that renews itself |
| PostgreSQL | in a container |
| `unattended-upgrades` | security patches without you |
| `fail2ban` | blocks the SSH knocking |

The one thing in the project that was licensed is not any more. Resizing
an avatar used ImageSharp, whose split licence grants Apache 2.0 to an
open-source project and to anyone under a million dollars a year - this is
both - but whose build task looks for a licence key and turns a warning
into an **error on a Release build**. That is a thing to discover while
deploying. It is SkiaSharp now, MIT with nothing to declare, carrying the
natives that need no fontconfig on the host, since nothing here draws any
text.

Certificates are free and come from Let's Encrypt, not from the host. With
Caddy a domain is a line in a file and you never think about renewal again;
with nginx you set up certbot and own the renewal yourself.

Take **Ubuntu LTS**, not because it is the best but because every answer
you will find at eleven at night assumes it. Windows Server is available
and costs a licence, and nothing here needs it: .NET runs on Linux and the
database is PostgreSQL. With Docker the host distribution barely matters
anyway.

Access is **SSH, not RDP** - a text terminal and no desktop. Installing a
desktop would eat the memory you paid for and widen the attack surface.
The thing that makes this comfortable from Windows is **VS Code with
Remote-SSH**: a file tree, an editor and a terminal, against the server, as
though the files were local.

Maintenance after the first evening is roughly half an hour a month: a
reboot when the kernel updates, and container images when a .NET or
Postgres release lands.

**Backups are the real difference between this and a managed service**, and
they are the one paragraph here that is not optional. A managed database
backs itself up and tests the restore; here a backup you have never
restored from is a hope. Losing a card game's tables costs nothing - they
are lost on every restart anyway. Losing an appointment book costs somebody
else their week, and the people in it are not customers of yours.

So: `pg_dump` on a schedule, to somewhere that is not this machine, and a
restore tried once before it is ever needed. Contabo sell an automatic
backup as an extra; it is worth taking and it is not a substitute for
having restored from one.

A middle road worth considering is to run the applications here and keep
the database at Neon or Supabase, managed and backed up on a free tier.
The game barely touches a database and would not notice. The appointment
book would be the reason to do it.

## Test environments

Four environments fit in 8 GB with room left - two applications twice over
plus Postgres is about 2.1 GB - but a test environment on the production
machine is not a test environment. The moment it exhausts memory or CPU,
which is exactly what you want to be free to do to it, it takes production
down with it. And production now has somebody else's business in it.

Run test on your own machine with Docker Desktop against the same compose
file. A second Contabo server is the other answer, but they bill by the
month rather than the hour, so an occasional test machine is better off
local.

If they do share a machine, give them separate databases rather than
separate schemas, and memory limits on the test containers so they cannot
starve production. Watch the disk as well - .NET images are about 200 MB each
and accumulate.

## A year, roughly

| | |
|---|---|
| server, Cloud VPS 4 at $5.61 on a year | ~₪240 |
| two domains | ~₪80 |
| | **~₪320** |

About a third of what Hetzner would now cost for half the machine, and
close to what this document said before any of its prices had been checked
- which is a coincidence and not a vindication.

## What has to be true first

These are not costs and they come before any of it.

**There is no mail sender.** Which is why `EmailConfirmed` is set at
registration and why there is no password reset - and in production a typo
in a password strands an account for good.

**Registration has no rate limit and no CAPTCHA.** It cannot require a
credential, so those are its only protection, and a public URL collects
bots.

**Secrets.** The database password lives in
`appsettings.Development.json` and the Google client secret under
`Secret/`. On a server they belong in environment variables or the host's
secret store. **This repository is public.**

**Google needs a redirect URI** registered for the real domain.

**`Program.cs` calls `UseHttpsRedirection` and has no
`UseForwardedHeaders`.** Behind a reverse proxy that terminates TLS, the
application sees `http`, redirects to `https`, the proxy forwards as `http`
again, and that is an infinite redirect loop on the first deployment.

## How to deploy

Configuration lives in the repository, not on the server. Deployment runs
from GitHub Actions on a push rather than from anybody's laptop. SSH is for
diagnosis.

That is better engineering and it is also what makes the server
disposable: if it burns, you rebuild it from the repository in a quarter of
an hour. The opposite - a server configured by hand over SSH - is a machine
whose state nobody can reproduce and nobody dares touch.
