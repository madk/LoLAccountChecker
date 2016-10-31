import os, sys, urllib, urllib2, json

api_version_url = 'https://ddragon.leagueoflegends.com/api/versions.json'
realm_version_url = 'https://ddragon.leagueoflegends.com/realms/na.json'

def loadJsonFromUrl(url):
	r = urllib2.urlopen(url)
	str_r = r.read().decode('utf-8')
	return json.loads(str_r)

print 'Latest API Version: ' + loadJsonFromUrl(api_version_url)[0]

obj = loadJsonFromUrl(realm_version_url)

champs_vers = obj['n']['champion']
runes_vers = obj['n']['rune']

base_url = 'http://ddragon.leagueoflegends.com/cdn/'
champs_url = '%s%s/data/en_US/championFull.json' % (base_url, champs_vers)
runes_url = '%s%s/data/en_US/rune.json' %(base_url, runes_vers)

print '\nRealm: NA'

print 'Champions: ' + champs_vers
print 'Runes: ' + runes_vers

print '\nDownload? y/n'
if raw_input() != 'y':
    sys.exit()

obj = loadJsonFromUrl(champs_url)

output = []

for ck, champion in sorted(obj['data'].items()):
    skins = []

    for skin in champion['skins']:
        skins.append({'Id': int(skin['id']), 'Name': skin['name'], 'Num': skin['num'], 'ChampionId': int(champion['key'])})

    output.append({'Id': int(champion['key']), 'Name': champion['id'], 'DisplayName': champion['name'], 'Skins': skins})

    print(champion['name'])


with open('Champions.json', 'w') as f:
	json.dump(output, f)

with open('Champions.Version', 'w') as f:
	f.write(champs_vers)

def getRuneType(str_type):
	type = {
		'red':		1,
		'yellow':	2,
		'blue':		3,
		'black':	4
	}
	return type[str_type] if str_type in type else 0


obj = loadJsonFromUrl(runes_url)

output = []

for rune_id in obj['data']:
	rune = {
		'Id':			int(rune_id),
		'Name':			obj['data'][rune_id]['name'],
		'Description':	obj['data'][rune_id]['description'],
		'Tier': 		int(obj['data'][rune_id]['rune']['tier']),
		'Type':			getRuneType(obj['data'][rune_id]['rune']['type'])
	}
	output.append(rune)
	
	print rune['Name']

with open('Runes.json', 'w') as f:
	json.dump(output, f)

with open('Runes.Version', 'w') as f:
	f.write(runes_vers)

print 'Press Enter to continue...' 
raw_input()
