// Vuex Store 模块模板
// 每个业务模块一个文件，使用 namespaced: true

const state = {
  list: [],
  currentItem: null,
  loading: false,
  pagination: {
    page: 1,
    limit: 20,
    total: 0
  }
};

const getters = {
  list: state => state.list,
  currentItem: state => state.currentItem,
  loading: state => state.loading,
  pagination: state => state.pagination
};

const mutations = {
  SET_LIST(state, list) {
    state.list = list;
  },
  SET_CURRENT_ITEM(state, item) {
    state.currentItem = item;
  },
  SET_LOADING(state, loading) {
    state.loading = loading;
  },
  SET_PAGINATION(state, pagination) {
    state.pagination = { ...state.pagination, ...pagination };
  },
  ADD_ITEM(state, item) {
    state.list.unshift(item);
  },
  UPDATE_ITEM(state, updatedItem) {
    const index = state.list.findIndex(item => item.id === updatedItem.id);
    if (index !== -1) {
      state.list.splice(index, 1, updatedItem);
    }
  },
  REMOVE_ITEM(state, id) {
    state.list = state.list.filter(item => item.id !== id);
  }
};

const actions = {
  async fetchList({ commit }, params) {
    commit('SET_LOADING', true);
    try {
      // const res = await api.getList(params);
      // commit('SET_LIST', res.data.list);
      // commit('SET_PAGINATION', { total: res.data.total });
    } catch (error) {
      throw error;
    } finally {
      commit('SET_LOADING', false);
    }
  },

  async fetchDetail({ commit }, id) {
    commit('SET_LOADING', true);
    try {
      // const res = await api.getDetail(id);
      // commit('SET_CURRENT_ITEM', res.data);
    } catch (error) {
      throw error;
    } finally {
      commit('SET_LOADING', false);
    }
  },

  async create({ commit }, data) {
    // const res = await api.create(data);
    // commit('ADD_ITEM', res.data);
  },

  async update({ commit }, { id, data }) {
    // const res = await api.update(id, data);
    // commit('UPDATE_ITEM', res.data);
  },

  async delete({ commit }, id) {
    // await api.delete(id);
    // commit('REMOVE_ITEM', id);
  }
};

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions
};
