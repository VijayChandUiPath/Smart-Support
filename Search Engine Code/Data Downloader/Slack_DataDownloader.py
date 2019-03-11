#!/usr/bin/env python
# coding: utf-8

# In[4]:


import json
import requests
from datetime import datetime


# In[5]:


oldest = '1514764800'
token = 'xoxp-3684779249-155352630070-511060201559-018541c61deedb575c67a2dfc9e5cd01'
channel = 'C765RQ6QK' 
file = './Output/Slack-Product-Support/Slack-help.txt'
# fantastic = C0A9PPS64
# infra = C9C4YCLT1
# help = C765RQ6QK
# Product-Support = C6GT9NYQY


# In[ ]:


while(True):
    channelHistory_api = 'https://slack.com/api/channels.history?channel='+channel+'&oldest='+oldest+'&pretty=1&token='+token+''
    #print("Oldest :::::::::::::::::::::::::::::::::::::::::::::::::::" + datetime.fromtimestamp(float(oldest)))
    req = requests.get(channelHistory_api)
    j = req.json()
    for msg in j['messages']:
        t = json.dumps(msg)
        t = json.loads(t)
        ts = msg['ts']
        print(datetime.fromtimestamp(float(ts)))

        permanentLink_api = 'https://slack.com/api/chat.getPermalink?channel='+channel+'&message_ts='+msg['ts']+'&pretty=1&token='+token+''
        req_permLink = requests.get(permanentLink_api)
        permLink = req_permLink.json()['permalink']
        #print(permLink)

        t['permalink'] = permLink
        with open(file, "a", encoding='utf-8') as myfile:
            myfile.write(str(t) + '\n')
    print('=====================================')

    if(j['has_more']):
        print(j['has_more'])
        oldest = j['messages'][0]['ts']
    else:
        break

