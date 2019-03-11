#!/usr/bin/env python
# coding: utf-8

# In[4]:


import simplejson as json
import pymongo


# In[12]:


myclient = pymongo.MongoClient("mongodb://localhost:27017/")
mydb = myclient["Slack"]
mycol = mydb["Slack-Product-Support"]


# In[ ]:


file = 'C:/Users/Vijay-VM/Desktop/Python/Data Downloader/Guide Data Downloader/Output/Slack/Slack-Product-Support.txt'

with open(file, encoding="utf8") as fileobject:
    for line in fileobject:
        #json_line = load_dirty_json(line)
        l = eval(line)
        json_line = json.dumps(l)
        json_line = json.loads(json_line)
        
        #print(json_line['text'] + " : " + json_line['ts'] + " : " + json_line['thread_ts'])
        data = {}
        data['text'] = json_line['text']
        data['ts'] = json_line['ts']
        data['permalink'] = json_line['permalink']
        try:
            data['thread_ts'] = json_line['thread_ts']
        except:
            pass
        x = mycol.insert_one(json.loads(json.dumps(data)))
        
        
        #x = mycol.insert_one(json_line)
        print(x.inserted_id)
        #print(json_line['ts'])
        


# In[ ]:





# In[ ]:





# In[1]:


# Ingest Zendesk Data


# In[2]:


import simplejson as json
import pymongo


# In[3]:


myclient = pymongo.MongoClient("mongodb://localhost:27017/")
mydb = myclient["Zendesk"]
mycol = mydb["Tickets-COnversation"]


# In[ ]:


file = 'F:/Zendesk Data/split/Ticket--June 1, 2017 -- December 15, 2018--345.json'
count = 0
with open(file, encoding="utf8") as fileobject:
    for line in fileobject:
        count = count + 1 
        json_line = json.loads(line)
        x = mycol.insert_one(json_line)
        print(str(count) + " : " + str(x.inserted_id))

