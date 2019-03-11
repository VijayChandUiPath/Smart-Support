#!/usr/bin/env python
# coding: utf-8

# In[6]:


import urllib
from bs4 import BeautifulSoup
from urllib.request import urlopen
import mechanize
import http.cookiejar
import pandas as pd


# In[7]:


filepath = 'C:/Users/Vijay-VM/Desktop/Python/Crawler/UiPath-Orchestrator/crawled.txt'
df = pd.read_fwf(filepath)


# In[8]:


ls_fail = []


# In[ ]:


for index, row in df.iterrows():
    downloadTextFromLink(row[0])


# In[9]:


def downloadTextFromLink(url):
    print('Downloading : ', url)
    try:
        
        if ".zip" not in url and ".exe" not in url:      
            # Browser
            br = mechanize.Browser()
            # Cookie Jar
            cj = http.cookiejar.LWPCookieJar()
            br.set_cookiejar(cj)

            # Browser options
            br.set_handle_equiv(True)
            br.set_handle_gzip(True)
            br.set_handle_redirect(True)
            br.set_handle_referer(True)
            br.set_handle_robots(False)

            # Follows refresh 0 but not hangs on refresh > 0
            br.set_handle_refresh(mechanize._http.HTTPRefreshProcessor(), max_time=1)

            # Want debugging messages?
            #br.set_debug_http(True)
            #br.set_debug_redirects(True)
            #br.set_debug_responses(True)

            # User-Agent (this is cheating, ok?)
            br.addheaders = [('User-agent', 'Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.1) Gecko/2008071615 Fedora/3.0.1-1.fc9 Firefox/3.0.1')]

            # Open some site, let's pick a random one, the first that pops in mind:
            try:
                r = br.open(url)
            except:
                r = br.open(url.replace("http","https"))
            html = r.read()        
            soup = BeautifulSoup(html)

            # kill all script and style elements
            for script in soup(["script", "style"]):
                script.extract()    # rip it out

            # get text
            text = soup.get_text()

            # break into lines and remove leading and trailing space on each
            lines = (line.strip() for line in text.splitlines())
            # break multi-headlines into a line each
            chunks = (phrase.strip() for line in lines for phrase in line.split("  "))
            # drop blank lines
            text = '\n'.join(chunk for chunk in chunks if chunk)

            # output to a text file
            with open("./Output/UiPath-Orchestrator/" +  url.split("://",1)[1].replace("/",".").replace("\\",".").replace(":",".").replace("*",".").replace("?",".").replace("\"",".").replace("<",".").replace(">",".").replace("|",".") + '.txt', "a") as text_file:
                text_file.write(text)
            print('Downloaded ', url)
            print('=====================================================================')
    except Exception as ex:
        print('-------------------------->Failed to scrap website : ' + url)
        print('-------------------------->Exception : ' + str(ex))
        ls_fail.append(url)
        

