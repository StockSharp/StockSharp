/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.AbstractSet;
import java.util.Iterator;
import java.util.concurrent.ConcurrentHashMap;

public class ConcurrentHashSet<Key> extends AbstractSet<Key> {
    static Object OBJECT = new Object();

    private ConcurrentHashMap<Key, Object> m_map = new ConcurrentHashMap<Key, Object>(16,0.75f,1); // use write concurrency level 1 (last param) to decrease memory consumption by ConcurrentHashMap

    /** return true if object was added as "first value" for this key */
    public boolean add( Key key) {
        return m_map.put( key, OBJECT) == null; // null means there was no value for given key previously
    }

    public boolean contains( Object key) {
        return m_map.containsKey( key);
    }

    public Iterator<Key> iterator() {
        return m_map.keySet().iterator();
    }

    /** return true if key was indeed removed */
    public boolean remove( Object key) {
        return m_map.remove( key) == OBJECT; // if value not null it was existing in the map
    }

    public boolean isEmpty() {
        return m_map.isEmpty();
    }

    public int size() {
        return m_map.size();
    }

    public void clear() {
        m_map.clear();
    }
}
