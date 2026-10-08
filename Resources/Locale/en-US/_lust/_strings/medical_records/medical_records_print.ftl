### Medical record print template
lust-medical-records-print-name = Medical record for { $name }
lust-medical-records-print-content =
    ​
    ​
    ​[head=1]NanoTrasen[/head]
    ​[bold]Employee medical record[/bold]
    ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

    ​[head=2]Personal information[/head]

    ​[bullet] [color=#594D4A][bold]Name:[/bold][/color] [italic]{ $name }[/italic]
    ​[bullet] [color=#594D4A][bold]Age:[/bold][/color] [italic]{ $age }[/italic]
    ​[bullet] [color=#594D4A][bold]Gender:[/bold][/color] [italic]{ $gender }[/italic]
    ​[bullet] [color=#594D4A][bold]Species:[/bold][/color] [italic]{ $species }[/italic]
    ​[bullet] [color=#594D4A][bold]Job:[/bold][/color] [italic]{ $job }[/italic]

    ​[head=2]Biometric data[/head]

    ​[bullet] [color=#5BA4CF][bold]Fingerprints:[/bold][/color] [mono]{ $fingerprint }[/mono]
    ​[bullet] [color=#5BA4CF][bold]DNA:[/bold][/color] [mono]{ $dna }[/mono]

    ​[head=2]Medical information[/head]

    ​[bullet] [color=#594D4A][bold]Close relatives:[/bold][/color] [italic]{ $closeRelatives }[/italic]
    ​[bullet] [color=#594D4A][bold]Emergency contact:[/bold][/color] [italic]{ $emergencyContact }[/italic]
    ​[bullet] [color=#594D4A][bold]Physiological details:[/bold][/color] [italic]{ $physiologicalTraits }[/italic]
    ​[bullet] [color=#594D4A][bold]Psychological details:[/bold][/color] [italic]{ $psychologicalTraits }[/italic]

    ​[head=2]Physician's notes[/head]

    ​[italic]{ $notes }[/italic]
