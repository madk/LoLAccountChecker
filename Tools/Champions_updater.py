import urllib.request, json

r = urllib.request.urlopen('http://ddragon.leagueoflegends.com/cdn/5.9.1/data/en_US/championFull.json')

str_r = r.readall().decode('utf-8')
obj = json.loads(str_r)

output = []

for ck,champion in sorted(obj['data'].items()):
	
	skins = []
	
	for c,skin in enumerate(champion['skins']):
		skins.append({'Id': int(skin['id']), 'Name': skin['name'], 'Number': c+1})
	
	output.append({'Id': int(champion['key']), 'Name': champion['name'], 'Skins': skins})
	print(champion['name'])
	
	
f = open('Champions.json', 'w')
json.dump(output, f)
f.close()
